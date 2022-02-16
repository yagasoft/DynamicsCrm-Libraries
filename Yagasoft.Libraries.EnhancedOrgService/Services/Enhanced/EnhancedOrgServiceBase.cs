#region Imports

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Xrm.Client.Services;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Yagasoft.Libraries.Common;
using Yagasoft.Libraries.EnhancedOrgService.Events;
using Yagasoft.Libraries.EnhancedOrgService.Events.EventArgs;
using Yagasoft.Libraries.EnhancedOrgService.Exceptions;
using Yagasoft.Libraries.EnhancedOrgService.Helpers;
using Yagasoft.Libraries.EnhancedOrgService.Params;
using Yagasoft.Libraries.EnhancedOrgService.Pools;
using Yagasoft.Libraries.EnhancedOrgService.Pools.WarmUp;
using Yagasoft.Libraries.EnhancedOrgService.Response.Operations;
using Yagasoft.Libraries.EnhancedOrgService.Response.Tokens;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced.Deferred;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced.Planned;
using Yagasoft.Libraries.EnhancedOrgService.Services.SelfDisposing;
using Yagasoft.Libraries.EnhancedOrgService.State;
using Yagasoft.Libraries.EnhancedOrgService.Transactions;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced
{
	/// <inheritdoc cref="IEnhancedOrgService" />
	public abstract class EnhancedOrgServiceBase : IStateful, IEnhancedOrgService
	{
		public virtual event EventHandler<IOrganizationService, OperationStatusEventArgs> OperationStatusChanged
		{
			add
			{
				InnerOperationStatusChanged -= value;
				InnerOperationStatusChanged += value;
			}
			remove => InnerOperationStatusChanged -= value;
		}

		protected virtual event EventHandler<IOrganizationService, OperationStatusEventArgs> InnerOperationStatusChanged;

		public virtual IEnumerable<Operation> PendingOperations => pendingOperations;
		public virtual IEnumerable<Operation> ExecutedOperations => executedOperations;

		public virtual IEnumerable<OrganizationRequest> DeferredRequests
		{
			get
			{
				ValidateDeferredQueueState();
				return deferredOrgService?.DeferredRequests;
			}
		}

		public virtual int RequestCount { get; protected internal set; }
		public virtual int FailureCount { get; protected internal set; }
		public virtual double FailureRate => FailureCount / (double)(RequestCount == 0 ? 1 : RequestCount);
		public virtual int RetryCount { get; protected internal set; }

		public ServiceParams Parameters { get; protected internal set; }

		protected internal virtual ITransactionManager TransactionManager { get; set; }

		protected internal static int OperationIndex;

		protected internal IOrganizationService CrmService;
		protected internal IServicePool<IOrganizationService> ServicePool;

		protected internal Action ReleaseService;

		protected internal virtual IOrganizationServiceCache Cache { get; set; }
		protected internal virtual ObjectCache ObjectCache { get; set; }
		protected internal virtual bool IsCacheEnabled => Parameters?.IsCachingEnabled == true && Cache != null;

		protected internal bool IsReleased = true;

		private readonly List<Operation> pendingOperations;
		private readonly FixedSizeQueue<Operation> executedOperations;

		private IDeferredOrgService deferredOrgService;
		private bool isUseSdkDeferredOperations;

		private IPlannedOrgService plannedOrgService;

		private TimeSpan? DequeueTimeout => Parameters?.PoolParams?.DequeueTimeout;

		private bool IsRetryEnabled => Parameters?.IsAutoRetryEnabled ?? false;
		private int MaxRetryCount => Parameters?.AutoRetryParams?.MaxRetryCount ?? 1;
		private TimeSpan RetryInterval => Parameters?.AutoRetryParams?.RetryInterval ?? TimeSpan.FromSeconds(5);
		private double RetryBackoffMultiplier => Parameters?.AutoRetryParams?.BackoffMultiplier ?? 1;
		private TimeSpan? MaxmimumRetryInterval => Parameters?.AutoRetryParams?.MaxmimumRetryInterval;

		private IEnumerable<Func<Func<IOrganizationService, object>, Operation, ExecuteParams, Exception, object>> CustomRetryFunctions
			=> Parameters?.AutoRetryParams?.CustomRetryFunctions;

		protected internal EnhancedOrgServiceBase(ServiceParams parameters)
		{
			Parameters = parameters;
			pendingOperations = new List<Operation>();
			executedOperations = new FixedSizeQueue<Operation>(parameters?.OperationHistoryLimit ?? int.MaxValue);
		}

		public virtual void ValidateState(bool isValid = true)
		{
			if (IsReleased)
			{
				throw new StateException("Service is not ready. Try to get a new service from the helper/pool/factory.");
			}
		}

		#region Service

		public virtual void WarmUp()
		{
			if (ServicePool != null)
			{
				ServicePool.WarmUp();
			}
		}

		public virtual void EndWarmup()
		{
			if (ServicePool != null)
			{
				ServicePool.EndWarmup();
			}
		}

		protected internal virtual void InitialiseConnection(IOrganizationService service)
		{
			service.Require(nameof(service));
			CrmService = service;
			IsReleased = false;
		}

		protected internal virtual void InitialiseConnection(IServicePool<IOrganizationService> servicePool)
		{
			servicePool.Require(nameof(servicePool));
			ServicePool = servicePool;
			IsReleased = false;
		}

		protected internal virtual IOrganizationService ClearConnection()
		{
			var service = CrmService;
			CrmService = null;
			return service;
		}

		protected internal virtual IDisposableService GetService()
		{
			ValidateState();

			var service = CrmService;

			if (ServicePool != null)
			{
				service = ServicePool.GetService() ?? CrmService;
			}

			if (service == null)
			{
				throw new StateException("Failed to find an internal CRM service.");
			}

			if (service.EnsureTokenValid() == false)
			{
				throw new SecurityTokenExpiredException("Service token has expired.");
			}

			return ServicePool == null
				? new SelfDisposingService(service, () => { })
				: new SelfDisposingService(service, () => ServicePool.ReleaseService(service));
		}

		#endregion

		#region Transactions

		public virtual Transaction BeginTransaction(string transactionId = null)
		{
			ValidateTransactionSupport();
			ValidateState();

			return TransactionManager.BeginTransaction(transactionId);
		}

		public virtual void UndoTransaction(Transaction transaction = null)
		{
			ValidateTransactionSupport();
			ValidateState();

			using var service = GetService();
			TransactionManager.UndoTransaction(service, transaction);
		}

		public virtual void AddUndoLogicToCache<TRequestType>(
			Func<IOrganizationService, OrganizationRequest, OrganizationRequest> undoFunction)
			where TRequestType : OrganizationRequest
		{
			ValidateTransactionSupport();
			ValidateState();

			var requestType = typeof(TRequestType);

			if (UndoHelper.UndoLogicCache.ContainsKey(requestType))
			{
				UndoHelper.UndoLogicCache.Remove(requestType);
			}

			UndoHelper.UndoLogicCache.Add(requestType, undoFunction);
		}

		public virtual void EndTransaction(Transaction transaction = null)
		{
			ValidateTransactionSupport();
			ValidateState();

			TransactionManager.EndTransaction(transaction);
		}

		protected virtual void ValidateTransactionSupport()
		{
			if (TransactionManager == null)
			{
				throw new NotSupportedException("Transactions are not enabled for this service.");
			}
		}

		protected virtual void AddToTransaction(Operation operation, ExecuteParams executeParams)
		{
			if (executeParams?.IsTransactionsEnabled != false && TransactionManager?.IsTransactionInEffect() == true)
			{
				using var service = GetService();
				TransactionManager.ProcessRequest(service, operation);
			}
		}

		#endregion

		protected virtual T InnerExecute<T>(OrganizationRequest request, Func<OrganizationResponse, T> selector, string selectorCacheKey)
		{
			var execute = Cache == null
				? null
				: new Func
					<OrganizationRequest, Func<OrganizationRequest, OrganizationResponse>, Func<OrganizationResponse, T>, string, T>
					(Cache.Execute);

			return InnerExecute(request, execute, selector, selectorCacheKey);
		}

		protected virtual T InnerExecute<T>(OrganizationRequest request) where T : OrganizationResponse
		{
			return InnerExecute(request, response => response as T, null);
		}

		protected virtual T InnerExecute<T>(OrganizationRequest request,
			Func<OrganizationRequest, Func<OrganizationRequest, OrganizationResponse>, Func<OrganizationResponse, T>, string, T>
				execute,
			Func<OrganizationResponse, T> selector, string selectorCacheKey)
		{
			return execute == null ? selector(InnerExecute(request)) : execute(request, InnerExecute, selector, selectorCacheKey);
		}

		protected virtual OrganizationResponse InnerExecute(OrganizationRequest request)
		{
			// TODO dead-lock potential: second call in a row
			using var service = GetService();
			return service.Execute(request is KeyedRequest keyedRequest ? keyedRequest.Request : request);
		}

		#region SDK Operations

		public virtual Guid Create(Entity entity)
		{
			return CreateAsOperation(entity).Response?.id ?? Guid.Empty;
		}

		public virtual void Update(Entity entity)
		{
			Update(entity, null);
		}

		public virtual void Delete(string entityName, Guid id)
		{
			Delete(entityName, id, null);
		}

		public virtual void Associate(string entityName, Guid entityId, Relationship relationship,
			EntityReferenceCollection relatedEntities)
		{
			Associate(entityName, entityId, relationship, relatedEntities, null);
		}

		public virtual void Disassociate(string entityName, Guid entityId, Relationship relationship,
			EntityReferenceCollection relatedEntities)
		{
			Disassociate(entityName, entityId, relationship, relatedEntities, null);
		}

		public virtual Entity Retrieve(string entityName, Guid id, ColumnSet columnSet)
		{
			return RetrieveAsOperation(entityName, id, columnSet).Response?.Entity
				?? new Entity(entityName, id);
		}

		public virtual EntityCollection RetrieveMultiple(QueryBase query)
		{
			return RetrieveMultipleAsOperation(query).Response?.EntityCollection
				?? new EntityCollection();
		}

		public virtual OrganizationResponse Execute(OrganizationRequest request)
		{
			return Execute(request, null, null);
		}

		#endregion

		#region Enhanced Operations

		public virtual Guid Create(Entity entity, ExecuteParams executeParams)
		{
			return CreateAsOperation(entity, executeParams).Response?.id ?? Guid.Empty;
		}

		public virtual Operation<UpdateResponse> Update(Entity entity, ExecuteParams executeParams)
		{
			return UpdateAsOperation(entity, executeParams);
		}

		public virtual Operation<DeleteResponse> Delete(string entityName, Guid id, ExecuteParams executeParams)
		{
			return DeleteAsOperation(entityName, id, executeParams);
		}

		public virtual Operation<AssociateResponse> Associate(string entityName, Guid entityId, Relationship relationship,
			EntityReferenceCollection relatedEntities, ExecuteParams executeParams)
		{
			return AssociateAsOperation(entityName, entityId, relationship, relatedEntities, executeParams);
		}

		public virtual Operation<DisassociateResponse> Disassociate(string entityName, Guid entityId, Relationship relationship,
			EntityReferenceCollection relatedEntities, ExecuteParams executeParams)
		{
			return DisassociateAsOperation(entityName, entityId, relationship, relatedEntities, executeParams);
		}

		public virtual Entity Retrieve(string entityName, Guid id, ColumnSet columnSet, ExecuteParams executeParams)
		{
			return RetrieveAsOperation(entityName, id, columnSet, executeParams).Response?.Entity
				?? new Entity(entityName, id);
		}

		public virtual TEntity Retrieve<TEntity>(string entityName, Guid id, ColumnSet columnSet,
			ExecuteParams executeParams = null)
			where TEntity : Entity
		{
			return Retrieve(entityName, id, columnSet, executeParams).ToEntity<TEntity>();
		}

		public virtual EntityCollection RetrieveMultiple(QueryBase query, ExecuteParams executeParams)
		{
			return RetrieveMultipleAsOperation(query, executeParams).Response?.EntityCollection
				?? new EntityCollection();
		}

		public virtual OrganizationResponse Execute(OrganizationRequest request, ExecuteParams executeParams)
		{
			return Execute(request, executeParams, null);
		}

		public virtual OrganizationResponse Execute(OrganizationRequest request, ExecuteParams executeParams,
			Func<IOrganizationService, OrganizationRequest, OrganizationRequest> undoFunction)
		{
			return ExecuteAsOperation(request, executeParams, undoFunction).Response;
		}

		public virtual Operation<CreateResponse> CreateAsOperation(Entity entity, ExecuteParams executeParams = null)
		{
			ValidateState();

			var request =
				new CreateRequest
				{
					Target = entity
				};
			var operation = PrepOperation<CreateResponse>(request);

			if (executeParams?.IsNotDeferred != true && isUseSdkDeferredOperations)
			{
				deferredOrgService.Require(nameof(deferredOrgService), "Deferred operations must be initialised first.");
				deferredOrgService.CreateDeferred(entity);
				return operation as Operation<CreateResponse>;
			}

			AddToTransaction(operation, executeParams);

			TryRunOperation(
				service =>
				{
					var result = service.Create(entity);
					operation.Response =
						new CreateResponse
						{
							[nameof(CreateResponse.id)] = result
						};
					return result;
				},
				operation, executeParams);

			if (IsCacheEnabled)
			{
				Cache.Remove(entity);
			}

			return operation as Operation<CreateResponse>;
		}

		public virtual Operation<UpdateResponse> UpdateAsOperation(Entity entity, ExecuteParams executeParams = null)
		{
			ValidateState();

			var request =
				new UpdateRequest
				{
					Target = entity
				};
			var operation = PrepOperation<UpdateResponse>(request);

			if (executeParams?.IsNotDeferred != true && isUseSdkDeferredOperations)
			{
				deferredOrgService.Require(nameof(deferredOrgService), "Deferred operations must be initialised first.");
				deferredOrgService.UpdateDeferred(entity);
				return operation as Operation<UpdateResponse>;
			}

			AddToTransaction(operation, executeParams);

			TryRunOperation(
				service =>
				{
					service.Update(entity);
					return 0;
				},
				operation, executeParams);

			operation.Response = new UpdateResponse();

			if (IsCacheEnabled)
			{
				Cache.Remove(entity);
			}

			return operation as Operation<UpdateResponse>;
		}

		public virtual Operation<DeleteResponse> DeleteAsOperation(string entityName, Guid id, ExecuteParams executeParams = null)
		{
			ValidateState();

			var request =
				new DeleteRequest
				{
					Target = new EntityReference(entityName, id)
				};
			var operation = PrepOperation<DeleteResponse>(request);

			if (executeParams?.IsNotDeferred != true && isUseSdkDeferredOperations)
			{
				deferredOrgService.Require(nameof(deferredOrgService), "Deferred operations must be initialised first.");
				deferredOrgService.DeleteDeferred(entityName, id);
				return operation as Operation<DeleteResponse>;
			}

			AddToTransaction(operation, executeParams);

			TryRunOperation(
				service =>
				{
					service.Delete(entityName, id);
					return 0;
				},
				operation, executeParams);

			operation.Response = new DeleteResponse();

			if (IsCacheEnabled)
			{
				Cache.Remove(entityName, id);
			}

			return operation as Operation<DeleteResponse>;
		}

		public virtual Operation<AssociateResponse> AssociateAsOperation(string entityName, Guid entityId, Relationship relationship,
			EntityReferenceCollection relatedEntities, ExecuteParams executeParams = null)
		{
			ValidateState();

			var request =
				new AssociateRequest
				{
					Target = new EntityReference(entityName, entityId),
					Relationship = relationship,
					RelatedEntities = relatedEntities
				};
			var operation = PrepOperation<AssociateResponse>(request);

			if (executeParams?.IsNotDeferred != true && isUseSdkDeferredOperations)
			{
				deferredOrgService.Require(nameof(deferredOrgService), "Deferred operations must be initialised first.");
				deferredOrgService.AssociateDeferred(entityName, entityId, relationship, relatedEntities);
				return operation as Operation<AssociateResponse>;
			}

			TryRunOperation(
				service =>
				{
					service.Associate(entityName, entityId, relationship, relatedEntities);
					return 0;
				},
				operation, executeParams);

			AddToTransaction(operation, executeParams);

			operation.Response = new AssociateResponse();

			return operation as Operation<AssociateResponse>;
		}

		public virtual Operation<DisassociateResponse> DisassociateAsOperation(string entityName, Guid entityId,
			Relationship relationship,
			EntityReferenceCollection relatedEntities, ExecuteParams executeParams = null)
		{
			ValidateState();

			var request =
				new DisassociateRequest
				{
					Target = new EntityReference(entityName, entityId),
					Relationship = relationship,
					RelatedEntities = relatedEntities
				};
			var operation = PrepOperation<DisassociateResponse>(request);

			if (executeParams?.IsNotDeferred != true && isUseSdkDeferredOperations)
			{
				deferredOrgService.Require(nameof(deferredOrgService), "Deferred operations must be initialised first.");
				deferredOrgService.DisassociateDeferred(entityName, entityId, relationship, relatedEntities);
				return operation as Operation<DisassociateResponse>;
			}

			TryRunOperation(
				service =>
				{
					service.Disassociate(entityName, entityId, relationship, relatedEntities);
					return 0;
				},
				operation, executeParams);

			AddToTransaction(operation, executeParams);

			operation.Response = new DisassociateResponse();

			return operation as Operation<DisassociateResponse>;
		}

		public virtual Operation<RetrieveResponse> RetrieveAsOperation(string entityName, Guid id, ColumnSet columnSet,
			ExecuteParams executeParams = null)
		{
			ValidateState();

			var request =
				new RetrieveRequest
				{
					Target =
						new EntityReference
						{
							LogicalName = entityName,
							Id = id
						},
					ColumnSet = columnSet
				};
			var operation = PrepOperation<RetrieveResponse>(request);

			var result =
				TryRunOperation(
					service =>
					{
						Entity resultInner;

						if (IsCacheEnabled && executeParams?.IsCachingEnabled != false)
						{
							resultInner = InnerExecute<RetrieveResponse>(request)?.Entity;
						}
						else
						{
							resultInner = service.Retrieve(entityName, id, columnSet);
						}

						return resultInner;
					},
					operation, executeParams);

			operation.Response =
				new RetrieveResponse
				{
					[nameof(RetrieveResponse.Entity)] = result
				};

			return operation as Operation<RetrieveResponse>;
		}

		public virtual Operation<RetrieveMultipleResponse> RetrieveMultipleAsOperation(QueryBase query,
			ExecuteParams executeParams = null)
		{
			ValidateState();

			var request =
				new RetrieveMultipleRequest
				{
					Query = query
				};
			var operation = PrepOperation<RetrieveMultipleResponse>(request);

			var result =
				TryRunOperation(
					service =>
					{
						EntityCollection resultInner;

						if (IsCacheEnabled && executeParams?.IsCachingEnabled != false)
						{
							resultInner = InnerExecute<RetrieveMultipleResponse>(request)?.EntityCollection;
						}
						else
						{
							resultInner = service.RetrieveMultiple(query);
						}

						return resultInner;
					},
					operation, executeParams);

			operation.Response =
				new RetrieveMultipleResponse
				{
					[nameof(RetrieveMultipleResponse.EntityCollection)] = result
				};

			return operation as Operation<RetrieveMultipleResponse>;
		}

		public virtual Operation ExecuteAsOperation(OrganizationRequest request, ExecuteParams executeParams = null)
		{
			return ExecuteAsOperation(request, executeParams, null);
		}

		public virtual Operation ExecuteAsOperation(OrganizationRequest request, ExecuteParams executeParams,
			Func<IOrganizationService, OrganizationRequest, OrganizationRequest> undoFunction)
		{
			ValidateState();
			return ExecuteAsOperation(request, executeParams, undoFunction,
				PrepOperation<OrganizationResponse>(request) as Operation<OrganizationResponse>);
		}

		public virtual Operation<TResponse> ExecuteAsOperation<TResponse>(OrganizationRequest request, ExecuteParams executeParams,
			Func<IOrganizationService, OrganizationRequest, OrganizationRequest> undoFunction)
			where TResponse : OrganizationResponse
		{
			ValidateState();
			return ExecuteAsOperation(request, executeParams, undoFunction, PrepOperation<TResponse>(request) as Operation<TResponse>);
		}

		protected virtual Operation<TResponse> ExecuteAsOperation<TResponse>(OrganizationRequest request, ExecuteParams executeParams,
			Func<IOrganizationService, OrganizationRequest, OrganizationRequest> undoFunction, Operation<TResponse> operation)
			where TResponse : OrganizationResponse
		{
			ValidateState();

			if (executeParams?.IsNotDeferred != true && isUseSdkDeferredOperations)
			{
				deferredOrgService.Require(nameof(deferredOrgService), "Deferred operations must be initialised first.");
				deferredOrgService.ExecuteDeferred<OrganizationResponse>(request);
				return null;
			}

			AddToTransaction(operation, executeParams);

			(operation as Operation).Response =
				TryRunOperation(
					service =>
					{
						OrganizationResponse resultInner;

						if (IsCacheEnabled && executeParams?.IsCachingEnabled != false)
						{
							resultInner = InnerExecute<OrganizationResponse>(request);
						}
						else
						{
							resultInner = service.Execute(request);
						}

						return resultInner;
					},
					operation, executeParams);

			return operation;
		}

		#endregion

		#region Deferred

		public virtual void StartSdkOpDeferredQueue()
		{
			StartDeferredQueueInner(true);
		}

		public virtual IDeferredOrgService StartDeferredQueue()
		{
			return StartDeferredQueueInner(false);
		}

		protected internal virtual IDeferredOrgService StartDeferredQueueInner(bool isSdk)
		{
			if (deferredOrgService == null)
			{
				isUseSdkDeferredOperations = isSdk;
				return deferredOrgService = new DeferredOrgService(this,
					() =>
					{
						deferredOrgService = null;
						isUseSdkDeferredOperations = false;
					});
			}

			throw new InitialisationException("Queue has already been started.");
		}

		protected virtual void ValidateDeferredQueueState()
		{
			if (deferredOrgService == null)
			{
				throw new StateException("Deferred queue is in an invalid state. Restart the queue first.");
			}
		}

		public virtual IDictionary<OrganizationRequest, OrganisationRequestToken<OrganizationResponse>> ExecuteDeferredRequests(
			int bulkSize = 1000)
		{
			ValidateDeferredQueueState();
			return deferredOrgService?.ExecuteDeferredRequests(bulkSize);
		}

		public virtual void CancelDeferredRequests()
		{
			deferredOrgService?.CancelDeferredRequests();
		}

		#endregion

		#region Planned Execution

		public virtual IPlannedOrgService StartExecutionPlanning()
		{
			if (plannedOrgService == null)
			{
				return plannedOrgService = new PlannedOrgService(this, () => plannedOrgService = null);
			}

			throw new InitialisationException("Planning has already been started.");
		}

		protected virtual void ValidateExecutionPlanState()
		{
			if (plannedOrgService == null)
			{
				throw new StateException("Execution planning is in an invalid state. Restart the plan first.");
			}
		}

		public virtual void CancelPlanning()
		{
			plannedOrgService?.CancelPlanning();
		}

		public virtual IDictionary<Guid, OrganizationResponse> ExecutePlan()
		{
			ValidateExecutionPlanState();
			return plannedOrgService?.ExecutePlan();
		}

		#endregion

		#region Convenience

		public virtual UpsertResponse Upsert(Entity entity, ExecuteParams executeParams = null)
		{
			return Execute<UpsertResponse>(
				new UpsertRequest
				{
					Target = entity
				}, executeParams);
		}

		public virtual TResponse Execute<TResponse>(OrganizationRequest request, ExecuteParams executeParams = null)
			where TResponse : OrganizationResponse
		{
			return (TResponse)Execute(request, executeParams, null);
		}

		public virtual TResponse Execute<TResponse, TRequest>(OrganizationRequest request, ExecuteParams executeParams = null,
			Func<IOrganizationService, TRequest, OrganizationRequest> undoFunction = null)
			where TResponse : OrganizationResponse
			where TRequest : OrganizationRequest
		{
			return (TResponse)Execute(request, executeParams,
				(Func<IOrganizationService, OrganizationRequest, OrganizationRequest>)undoFunction);
		}

		public virtual Operation<TResponse> ExecuteAsOperation<TResponse>(OrganizationRequest request,
			ExecuteParams executeParams = null)
			where TResponse : OrganizationResponse
		{
			return ExecuteAsOperation<TResponse>(request, executeParams, null);
		}

		public virtual Operation<TResponse> ExecuteAsOperation<TResponse, TRequest>(OrganizationRequest request,
			ExecuteParams executeParams = null, Func<IOrganizationService, TRequest, OrganizationRequest> undoFunction = null)
			where TRequest : OrganizationRequest
			where TResponse : OrganizationResponse
		{
			return ExecuteAsOperation(request, executeParams,
				(Func<IOrganizationService, OrganizationRequest, OrganizationRequest>)undoFunction) as Operation<TResponse>;
		}

		public virtual IDictionary<OrganizationRequest, ExecuteBulkResponse> ExecuteBulk(List<OrganizationRequest> requests,
			bool isReturnResponses = false, int batchSize = 1000, bool isContinueOnError = true,
			Action<int, int, IDictionary<OrganizationRequest, ExecuteBulkResponse>> bulkFinishHandler = null,
			ExecuteParams executeParams = null)
		{
			ValidateState();

			batchSize.RequireAbove(0, "batchSize");
			batchSize.RequireInRange(1, 1000, "batchSize");

			var bulkRequest =
				new ExecuteMultipleRequest
				{
					Requests = new OrganizationRequestCollection(),
					Settings =
						new ExecuteMultipleSettings
						{
							ContinueOnError = isContinueOnError,
							ReturnResponses = isReturnResponses
						}
				};

			var responses = new Dictionary<OrganizationRequest, ExecuteBulkResponse>();
			var perBulkResponses = new Dictionary<OrganizationRequest, ExecuteBulkResponse>();

			var batchCount = Math.Ceiling(requests.Count / (double)batchSize);

			// take bulk size only for each iteration
			for (var i = 0; i < batchCount; i++)
			{
				// clear the previous batch
				bulkRequest.Requests.Clear();
				perBulkResponses.Clear();

				// take batches
				bulkRequest.Requests.AddRange(requests.Skip(i * batchSize).Take(batchSize));

				var operation = ExecuteAsOperation<ExecuteMultipleResponse>(bulkRequest, executeParams);
				var bulkResponses = operation?.Response;

				if (bulkResponses == null)
				{
					continue;
				}

				var innerRequests = bulkRequest.Requests;

				bulkRequest =
					new ExecuteMultipleRequest
					{
						Requests = new OrganizationRequestCollection(),
						Settings =
							new ExecuteMultipleSettings
							{
								ContinueOnError = isContinueOnError,
								ReturnResponses = isReturnResponses
							}
					};

				// no need to build a response
				if (!isReturnResponses)
				{
					// break on error and no 'continue on error' option
					if (!isContinueOnError && (bulkResponses.IsFaulted
						|| bulkResponses.Responses.Any(e => e.Fault != null)))
					{
						break;
					}

					bulkFinishHandler?.Invoke(i + 1, (int)batchCount, perBulkResponses);
					continue;
				}

				for (var j = 0; j < bulkResponses.Responses.Count; j++)
				{
					var request = innerRequests[j];
					var bulkResponse = bulkResponses.Responses[j];
					var fault = bulkResponse.Fault;
					string faultMessage = null;

					// build fault message
					if (fault != null)
					{
						var builder = new StringBuilder();
						builder.AppendFormat("Message: \"{0}\", code: {1}", fault.Message, fault.ErrorCode);

						if (fault.TraceText != null)
						{
							builder.AppendFormat(", trace: \"{0}\"", fault.TraceText);
						}

						if (fault.InnerFault != null)
						{
							builder.AppendFormat(", inner message: \"{0}\", inner code: {1}", fault.InnerFault.Message,
								fault.InnerFault.ErrorCode);

							if (fault.InnerFault.TraceText != null)
							{
								builder.AppendFormat(", trace: \"{0}\"", fault.InnerFault.TraceText);
							}
						}

						faultMessage = builder.ToString();
					}

					var response =
						new ExecuteBulkResponse
						{
							RequestType = request.GetType(),
							Response = bulkResponse.Response,
							ResponseType = bulkResponse.Response?.GetType(),
							Fault = fault,
							FaultMessage = faultMessage
						};
					responses[request] = response;
					perBulkResponses[request] = response;
				}

				bulkFinishHandler?.Invoke(i + 1, (int)batchCount, perBulkResponses);

				// break on error and no 'continue on error' option
				if (!isContinueOnError && (bulkResponses.IsFaulted || bulkResponses.Responses.Any(e => e.Fault != null)))
				{
					break;
				}
			}

			return responses;
		}

		#region Retrieve multiple

		public virtual IEnumerable<TEntityType> RetrieveMultiple<TEntityType>(string query, int limit = -1,
			ExecuteParams executeParams = null)
			where TEntityType : Entity
		{
			ValidateState();
			
			return RetrieveMultiple<TEntityType>(RequestHelper.CloneQuery(this, null, query), limit, executeParams);
		}

		public virtual IEnumerable<TEntityType> RetrieveMultiple<TEntityType>(QueryExpression query, int limit = -1,
			ExecuteParams executeParams = null)
			where TEntityType : Entity
		{
			ValidateState();

			return RetrieveMultipleRangePaged<TEntityType>(query, 1,
				limit <= 0 ? int.MaxValue : (int)Math.Ceiling(limit / 5000d),
				limit <= 0 ? int.MaxValue : limit, executeParams);
		}

		public virtual IEnumerable<TEntityType> RetrieveMultipleRangePaged<TEntityType>(QueryExpression query,
			int pageStart = 1, int pageEnd = 1, int pageSize = 5000, ExecuteParams executeParams = null)
			where TEntityType : Entity
		{
			ValidateState();

			pageStart.RequireAtLeast(1, nameof(pageStart));
			pageEnd.RequireAtLeast(1, nameof(pageEnd));

			var entities = new List<TEntityType>();

			for (var i = pageStart; i <= pageEnd; i++)
			{
				var result = RetrieveMultiplePage<TEntityType>(query, pageSize, i, executeParams)
					.ToArray();
				entities.AddRange(result);

				if (!result.Any())
				{
					break;
				}
			}

			return entities;
		}

		public virtual IEnumerable<TEntityType> RetrieveMultiplePage<TEntityType>(QueryExpression query, int pageSize = 5000,
			int page = 1,
			ExecuteParams executeParams = null)
			where TEntityType : Entity
		{
			ValidateState();

			page.RequireAtLeast(1, nameof(page));
			page.RequireInRange(0, 5000, nameof(pageSize));

			query.PageInfo ??= new PagingInfo();
			query.PageInfo.Count = pageSize;
			query.PageInfo.PageNumber = page;

			var currentCookie = query.PageInfo.PagingCookie;

			if (page > 1
				&& (currentCookie == null
					|| page - 1 != int.Parse(Regex.Match(currentCookie, @"page=""(.*?)""").Groups[1].ToString())))
			{
				query.PageInfo.PagingCookie = RequestHelper.GetCookie(this, query, pageSize, page);
			}

			var result = RetrieveMultipleAsOperation(query, executeParams)?.Response?.EntityCollection;

			if (result == null)
			{
				return new List<TEntityType>();
			}

			query.PageInfo.PagingCookie = result.PagingCookie;

			return result.Entities.Select(entity => entity.ToEntity<TEntityType>()).ToList();
		}

		#endregion

		#endregion

		#region Utility

		public virtual int GetRecordsCount(QueryBase query)
		{
			ValidateState();
			return RequestHelper.GetTotalRecordsCount(this, query);
		}

		public virtual int GetPagesCount(QueryBase query, int pageSize = 5000)
		{
			ValidateState();
			pageSize.RequireInRange(1, 5000, nameof(pageSize));
			return RequestHelper.GetTotalPagesCount(this, query, pageSize);
		}

		public virtual QueryExpression CloneQuery(QueryBase query)
		{
			ValidateState();
			return RequestHelper.CloneQuery(this, query);
		}

		#endregion

		#region Operation Handling

		protected internal virtual TResult TryRunOperation<TResult>(Func<IOrganizationService, TResult> action,
			Operation operation, ExecuteParams executeParams, bool isDelegated = false)
		{
			var isRetryEnabled = executeParams?.IsAutoRetryEnabled ?? IsRetryEnabled;
			var maxRetryCount = executeParams?.AutoRetryParams?.MaxRetryCount ?? MaxRetryCount;
			var retryInterval = executeParams?.AutoRetryParams?.RetryInterval ?? RetryInterval;
			var backoffMultiplier = executeParams?.AutoRetryParams?.BackoffMultiplier ?? RetryBackoffMultiplier;
			var maxmimumRetryInterval = executeParams?.AutoRetryParams?.MaxmimumRetryInterval ?? MaxmimumRetryInterval;

			var currentRetry = 0;
			var nextInterval = retryInterval;

			try
			{
				if (!isDelegated)
				{
					lock (pendingOperations)
					{
						pendingOperations.Add(operation); 
					}
				}

				while (true)
				{
					try
					{
						operation.OperationStatus = Status.InProgress;

						using (var service = GetService())
						{
							return action(service);
						}
					}
					catch (Exception ex)
					{
						if (!isRetryEnabled || currentRetry >= maxRetryCount || nextInterval > maxmimumRetryInterval)
						{
							if (!isDelegated)
							{
								FailureCount++;

								operation.Exception = ex;

								if (CustomRetryFunctions != null)
								{
									foreach (var function in CustomRetryFunctions)
									{
										try
										{
											var customRetryResult = function(s => action(s), operation, executeParams, ex);

											if (customRetryResult is TResult result && operation.OperationStatus == Status.Success)
											{
												return result;
											}
										}
										catch
										{
											// ignored
										}
									}
								}
							}

							throw;
						}

						operation.OperationStatus = Status.Retry;

						Task.Delay(nextInterval).Wait();
						nextInterval = new TimeSpan((long)(nextInterval.Ticks * backoffMultiplier));

						currentRetry++;

						if (!isDelegated)
						{
							RetryCount++;
						}
					}
				}
			}
			finally
			{
				if (!isDelegated)
				{
					RequestCount++;

					lock (pendingOperations)
					{
						pendingOperations.Remove(operation);
					}

					if (executeParams?.IsExcludeFromHistory != true)
					{
						lock (executedOperations)
						{
							executedOperations.Enqueue(operation);
						}
					}
				}

				operation.ClearEventHandlers();
			}
		}

		protected virtual void OnOperationStatusChanged(OperationStatusEventArgs e)
		{
			InnerOperationStatusChanged?.Invoke(this, e);
		}

		protected virtual Operation PrepOperation<TResponse>(OrganizationRequest request) where TResponse : OrganizationResponse
		{
			var operation =
				new Operation<TResponse>(request)
				{
					Index = ++OperationIndex
				};
			operation.OperationStatusChanged += (s, e) => OnOperationStatusChanged(e);
			operation.OperationStatus = Status.Ready;
			return operation;
		}

		#endregion

		public virtual void Dispose()
		{
			IsReleased = true;
			InnerOperationStatusChanged = null;
			CancelPlanning();
			CancelDeferredRequests();
			ReleaseService?.Invoke();
		}
	}
}
