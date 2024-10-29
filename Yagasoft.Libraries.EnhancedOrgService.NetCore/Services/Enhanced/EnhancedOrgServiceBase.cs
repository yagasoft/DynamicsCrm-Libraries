#region Imports

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Runtime.Caching;
using System.ServiceModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.PowerPlatform.Dataverse.Client.Utils;
using Microsoft.Rest;
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
using Yagasoft.Libraries.EnhancedOrgService.Response.Operations;
using Yagasoft.Libraries.EnhancedOrgService.Response.Tokens;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced.Deferred;
using Yagasoft.Libraries.EnhancedOrgService.Services.SelfDisposing;
using Yagasoft.Libraries.EnhancedOrgService.State;
using Yagasoft.Libraries.EnhancedOrgService.Transactions;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced
{
	/// <inheritdoc cref="IEnhancedOrgService" />
	public abstract class EnhancedOrgServiceBase : IStateful, IEnhancedOrgService
	{
		public virtual event EventHandler<IOrganizationService, OperationStatusEventArgs, IOrganizationService> OperationStatusChanged
		{
			add
			{
				InnerOperationStatusChanged -= value;
				InnerOperationStatusChanged += value;
			}
			remove => InnerOperationStatusChanged -= value;
		}

		protected virtual event EventHandler<IOrganizationService, OperationStatusEventArgs, IOrganizationService> InnerOperationStatusChanged;

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
		public IServicePool<IOrganizationService>? ServicePool;

		protected internal Func<Task> ReleaseService;

		protected internal virtual IOrganizationServiceCache Cache { get; set; }
		protected internal virtual ObjectCache ObjectCache { get; set; }
		protected internal virtual bool IsCacheEnabled => Parameters?.IsCachingEnabled == true && Cache != null;

		protected internal bool IsReleased = true;

		private readonly List<Operation> pendingOperations;
		private readonly FixedSizeQueue<Operation> executedOperations;
		private readonly SemaphoreSlim opsSemaphore = new (1);

		private IDeferredOrgService deferredOrgService;
		private bool isUseSdkDeferredOperations;

		private TimeSpan? DequeueTimeout => Parameters?.PoolParams?.DequeueTimeout;

		private bool IsRetryEnabled => Parameters?.IsAutoRetryEnabled ?? false;
		private int MaxRetryCount => Parameters?.AutoRetryParams?.MaxRetryCount ?? 1;
		private TimeSpan RetryInterval => Parameters?.AutoRetryParams?.RetryInterval ?? TimeSpan.FromSeconds(5);
		private double RetryBackoffMultiplier => Parameters?.AutoRetryParams?.BackoffMultiplier ?? 1;
		private TimeSpan? MaxmimumRetryInterval => Parameters?.AutoRetryParams?.MaxmimumRetryInterval;

		private IEnumerable<Func<Func<IOrganizationServiceAsync2, Task<object>>, Operation, ExecuteParams?, Exception?, Task<object>>>? CustomRetryFunctions
			=> Parameters?.AutoRetryParams?.CustomRetryFunctions;

		protected internal EnhancedOrgServiceBase(ServiceParams parameters)
		{
			Parameters = parameters;
			pendingOperations = new List<Operation>();

			var limit = parameters?.OperationHistoryLimit ?? 0;
			executedOperations = new FixedSizeQueue<Operation>(limit == 0 ? 1 : limit);
		}

		public virtual void ValidateState(bool isValid = true)
		{
			if (IsReleased)
			{
				throw new StateException("Service is not ready. Try to get a new service from the helper/pool/factory.");
			}
		}

		#region Service

		protected internal virtual void InitialiseConnection(IOrganizationService service)
		{
			service.Require(nameof(service));
			CrmService = service as IOrganizationService;
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

		protected internal virtual async Task<IDisposableService> GetService()
		{
			ValidateState();

			if (ServicePool != null)
			{
				return await ServicePool.GetSelfDisposingService();
			}

			return new SelfDisposingService(CrmService, async () => await Task.CompletedTask);
		}

		#endregion

		#region Transactions

		public virtual Transaction BeginTransaction(string transactionId = null)
		{
			ValidateTransactionSupport();
			ValidateState();

			return TransactionManager.BeginTransaction(transactionId);
		}

		public virtual async Task UndoTransaction(Transaction transaction = null)
		{
			ValidateTransactionSupport();
			ValidateState();

			using var service = await GetService();
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

		protected virtual async Task AddToTransaction(Operation operation, ExecuteParams executeParams)
		{
			if (executeParams?.IsTransactionsEnabled != false && TransactionManager?.IsTransactionInEffect() == true)
			{
				using var service = await GetService();
				TransactionManager.ProcessRequest(service, operation);
			}
		}

		#endregion

		protected virtual async Task<T> InnerExecute<T>(OrganizationRequest request, Func<OrganizationResponse, T> selector, string selectorCacheKey)
		{
			//var execute = Cache == null
			//	? null
			//	: new Func
			//		<OrganizationRequest, Func<OrganizationRequest, Task<OrganizationResponse>>, Func<OrganizationResponse, T>, string, Task<T>>
			//		(Cache.Execute);

			return await InnerExecute(request, null, selector, selectorCacheKey);
		}

		protected virtual async Task<T> InnerExecute<T>(OrganizationRequest request) where T : OrganizationResponse
		{
			return await InnerExecute(request, response => response as T, null);
		}

		protected virtual async Task<T> InnerExecute<T>(OrganizationRequest request,
			Func<OrganizationRequest, Func<OrganizationRequest, Task<OrganizationResponse>>, Func<OrganizationResponse, T>, string, Task<T>>
				execute,
			Func<OrganizationResponse, T> selector, string selectorCacheKey)
		{
			return execute == null ? selector(await InnerExecute(request)) : await execute(request, InnerExecute, selector, selectorCacheKey);
		}

		protected virtual async Task<OrganizationResponse> InnerExecute(OrganizationRequest request)
		{
			using var service = await GetService();
			return await service.ExecuteAsync(request is KeyedRequest keyedRequest ? keyedRequest.Request : request);
		}

		#region SDK Operations

		public virtual Guid Create(Entity entity)
		{
			return CreateAsync(entity).Result;
		}

		public virtual void Update(Entity entity)
		{
			UpdateAsync(entity).Wait();
		}

		public virtual void Delete(string entityName, Guid id)
		{
			DeleteAsync(entityName, id).Wait();
		}

		public virtual void Associate(string entityName, Guid entityId, Relationship relationship,
			EntityReferenceCollection relatedEntities)
		{
			AssociateAsync(entityName, entityId, relationship, relatedEntities).Wait();
		}

		public virtual void Disassociate(string entityName, Guid entityId, Relationship relationship,
			EntityReferenceCollection relatedEntities)
		{
			DisassociateAsync(entityName, entityId, relationship, relatedEntities).Wait();
		}

		public virtual Entity Retrieve(string entityName, Guid id, ColumnSet columnSet)
		{
			return RetrieveAsync(entityName, id, columnSet).Result;
		}

		public virtual EntityCollection RetrieveMultiple(QueryBase query)
		{
			return RetrieveMultipleAsync(query).Result;
		}

		public virtual OrganizationResponse Execute(OrganizationRequest request)
		{
			return ExecuteAsync(request).Result;
		}

		#endregion

		#region Async

		public async Task<Guid> CreateAsync(Entity entity)
		{
			return await CreateAsync(entity, CancellationToken.None);
		}

		public async Task<Entity> RetrieveAsync(string entityName, Guid id, ColumnSet columnSet)
		{
			return await RetrieveAsync(entityName, id, columnSet, CancellationToken.None);
		}

		public async Task UpdateAsync(Entity entity)
		{
			await UpdateAsync(entity, CancellationToken.None);
		}

		public async Task DeleteAsync(string entityName, Guid id)
		{
			await DeleteAsync(entityName, id, CancellationToken.None);
		}

		public async Task<OrganizationResponse> ExecuteAsync(OrganizationRequest request)
		{
			return await ExecuteAsync(request, null, null, CancellationToken.None);
		}

		public async Task AssociateAsync(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
		{
			await AssociateAsync(entityName, entityId, relationship, relatedEntities, CancellationToken.None);
		}

		public async Task DisassociateAsync(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
		{
			await DisassociateAsync(entityName, entityId, relationship, relatedEntities, CancellationToken.None);
		}

		public async Task<EntityCollection> RetrieveMultipleAsync(QueryBase query)
		{
			return await RetrieveMultipleAsync(query, CancellationToken.None);
		}

		public async Task AssociateAsync(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities,
			CancellationToken cancellationToken)
		{
			await AssociateAsync(entityName, entityId, relationship, relatedEntities, null, cancellationToken);
		}

		public async Task<Guid> CreateAsync(Entity entity, CancellationToken cancellationToken)
		{
			return await CreateAsync(entity, null, cancellationToken);
		}

		public async Task<Entity> CreateAndReturnAsync(Entity entity, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public async Task DeleteAsync(string entityName, Guid id, CancellationToken cancellationToken)
		{
			await DeleteAsync(entityName, id, null, cancellationToken);
		}

		public async Task DisassociateAsync(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities,
			CancellationToken cancellationToken)
		{
			await DisassociateAsync(entityName, entityId, relationship, relatedEntities, null, cancellationToken);
		}

		public async Task<OrganizationResponse> ExecuteAsync(OrganizationRequest request, CancellationToken cancellationToken)
		{
			return await ExecuteAsync(request, null, null, cancellationToken);
		}

		public async Task<Entity> RetrieveAsync(string entityName, Guid id, ColumnSet columnSet, CancellationToken cancellationToken)
		{
			return await RetrieveAsync<Entity>(entityName, id, columnSet, null, cancellationToken);
		}

		public async Task<EntityCollection> RetrieveMultipleAsync(QueryBase query, CancellationToken cancellationToken)
		{
			return await RetrieveMultipleAsync(query, null, cancellationToken);
		}

		public async Task UpdateAsync(Entity entity, CancellationToken cancellationToken)
		{
			await UpdateAsync(entity, null, cancellationToken);
		}
		
		#endregion

		#region Enhanced Operations

		public virtual Guid Create(Entity entity, ExecuteParams executeParams)
		{
			return CreateAsync(entity, executeParams, CancellationToken.None).Result;
		}

		public virtual async Task<Guid> CreateAsync(Entity entity, ExecuteParams executeParams, CancellationToken cancellationToken = default)
		{
			return (await CreateAsOperationAsync(entity, executeParams, cancellationToken)).Response?.id ?? Guid.Empty;
		}

		public virtual Operation<UpdateResponse> Update(Entity entity, ExecuteParams executeParams)
		{
			return UpdateAsync(entity, executeParams, CancellationToken.None).Result;
		}

		public virtual async Task<Operation<UpdateResponse>> UpdateAsync(Entity entity, ExecuteParams executeParams,
			CancellationToken cancellationToken = default)
		{
			return await UpdateAsOperationAsync(entity, executeParams, cancellationToken);
		}

		public virtual Operation<DeleteResponse> Delete(string entityName, Guid id, ExecuteParams executeParams)
		{
			return DeleteAsync(entityName, id, executeParams, CancellationToken.None).Result;
		}

		public virtual async Task<Operation<DeleteResponse>> DeleteAsync(string entityName, Guid id, ExecuteParams executeParams,
			CancellationToken cancellationToken = default)
		{
			return await DeleteAsOperationAsync(entityName, id, executeParams, cancellationToken);
		}

		public virtual Operation<AssociateResponse> Associate(string entityName, Guid entityId, Relationship relationship,
			EntityReferenceCollection relatedEntities, ExecuteParams executeParams)
		{
			return AssociateAsync(entityName, entityId, relationship, relatedEntities, executeParams, CancellationToken.None).Result;
		}

		public virtual async Task<Operation<AssociateResponse>> AssociateAsync(string entityName, Guid entityId,
			Relationship relationship,
			EntityReferenceCollection relatedEntities, ExecuteParams executeParams, CancellationToken cancellationToken = default)
		{
			return await AssociateAsOperationAsync(entityName, entityId, relationship, relatedEntities, executeParams, cancellationToken);
		}

		public virtual Operation<DisassociateResponse> Disassociate(string entityName, Guid entityId, Relationship relationship,
			EntityReferenceCollection relatedEntities, ExecuteParams executeParams)
		{
			return DisassociateAsync(entityName, entityId, relationship, relatedEntities, executeParams, CancellationToken.None).Result;
		}

		public virtual async Task<Operation<DisassociateResponse>> DisassociateAsync(string entityName, Guid entityId,
			Relationship relationship,
			EntityReferenceCollection relatedEntities, ExecuteParams executeParams, CancellationToken cancellationToken = default)
		{
			return await DisassociateAsOperationAsync(entityName, entityId, relationship, relatedEntities, executeParams, cancellationToken);
		}

		public virtual Entity Retrieve(string entityName, Guid id, ColumnSet columnSet, ExecuteParams executeParams)
		{
			return RetrieveAsync(entityName, id, columnSet, executeParams, CancellationToken.None).Result;
		}

		public virtual async Task<Entity> RetrieveAsync(string entityName, Guid id, ColumnSet columnSet, ExecuteParams executeParams, CancellationToken cancellationToken = default)
		{
			return (await RetrieveAsOperationAsync(entityName, id, columnSet, executeParams, cancellationToken)).Response?.Entity
				?? new Entity(entityName, id);
		}

		public virtual TEntity Retrieve<TEntity>(string entityName, Guid id, ColumnSet columnSet,
			ExecuteParams executeParams = null)
			where TEntity : Entity
		{
			return RetrieveAsync<TEntity>(entityName, id, columnSet, executeParams, CancellationToken.None).Result;
		}

		public virtual async Task<TEntity> RetrieveAsync<TEntity>(string entityName, Guid id, ColumnSet columnSet,
			ExecuteParams executeParams = null, CancellationToken cancellationToken = default)
			where TEntity : Entity
		{
			return (await RetrieveAsync(entityName, id, columnSet, executeParams, cancellationToken)).ToEntity<TEntity>();
		}

		public virtual EntityCollection RetrieveMultiple(QueryBase query, ExecuteParams executeParams)
		{
			return RetrieveMultipleAsync(query, executeParams, CancellationToken.None).Result;
		}

		public virtual async Task<EntityCollection> RetrieveMultipleAsync(QueryBase query, ExecuteParams executeParams, CancellationToken cancellationToken = default)
		{
			return (await RetrieveMultipleAsOperationAsync(query, executeParams, cancellationToken)).Response?.EntityCollection
				?? new EntityCollection();
		}

		public virtual OrganizationResponse Execute(OrganizationRequest request, ExecuteParams executeParams)
		{
			return ExecuteAsync(request, executeParams, CancellationToken.None).Result;
		}

		public virtual async Task<OrganizationResponse> ExecuteAsync(OrganizationRequest request, ExecuteParams executeParams, CancellationToken cancellationToken = default)
		{
			return await ExecuteAsync(request, executeParams, null, cancellationToken);
		}

		public virtual OrganizationResponse Execute(OrganizationRequest request, ExecuteParams executeParams,
			Func<IOrganizationService, OrganizationRequest, OrganizationRequest> undoFunction)
		{
			return ExecuteAsync(request, executeParams, undoFunction, CancellationToken.None).Result;
		}

		public virtual async Task<OrganizationResponse> ExecuteAsync(OrganizationRequest request, ExecuteParams executeParams,
			Func<IOrganizationService, OrganizationRequest, OrganizationRequest> undoFunction, CancellationToken cancellationToken = default)
		{
			return (await ExecuteAsOperationAsync(request, executeParams, undoFunction, cancellationToken)).Response;
		}

		public Operation<CreateResponse> CreateAsOperation(Entity entity, ExecuteParams executeParams = null)
		{
			return CreateAsOperationAsync(entity, executeParams).Result;
		}

		public virtual async Task<Operation<CreateResponse>> CreateAsOperationAsync(Entity entity, ExecuteParams executeParams = null,
			CancellationToken cancellationToken = default)
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

			await TryRunOperation(
				async service =>
				{
					var result = await service.CreateAsync(entity,
						cancellationToken == default ? CancellationToken.None : cancellationToken);
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
			return UpdateAsOperationAsync(entity, executeParams).Result;
		}

		public virtual async Task<Operation<UpdateResponse>> UpdateAsOperationAsync(Entity entity, ExecuteParams executeParams = null,
			CancellationToken cancellationToken = default)
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

			await TryRunOperation(
				async service =>
				{
					await service.UpdateAsync(entity,
						cancellationToken == default ? CancellationToken.None : cancellationToken);
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

		public virtual Operation<DeleteResponse> DeleteAsOperation(string entityName, Guid id,
			ExecuteParams executeParams = null)
		{
			return DeleteAsOperationAsync(entityName, id, executeParams, CancellationToken.None).Result;
		}

		public virtual async Task<Operation<DeleteResponse>> DeleteAsOperationAsync(string entityName, Guid id,
			ExecuteParams executeParams = null, CancellationToken cancellationToken = default)
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

			await TryRunOperation(
				async service =>
				{
					await service.DeleteAsync(entityName, id,
						cancellationToken == default ? CancellationToken.None : cancellationToken);
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
			return AssociateAsOperationAsync(entityName, entityId, relationship, relatedEntities, executeParams, CancellationToken.None).Result;
		}

		public virtual async Task<Operation<AssociateResponse>> AssociateAsOperationAsync(string entityName, Guid entityId,
			Relationship relationship,
			EntityReferenceCollection relatedEntities, ExecuteParams executeParams = null, CancellationToken cancellationToken = default)
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

			await TryRunOperation(
				async service =>
				{
					await service.AssociateAsync(entityName, entityId, relationship, relatedEntities,
						cancellationToken == default ? CancellationToken.None : cancellationToken);
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
			return DisassociateAsOperationAsync(entityName, entityId, relationship, relatedEntities, executeParams, CancellationToken.None).Result;
		}

		public virtual async Task<Operation<DisassociateResponse>> DisassociateAsOperationAsync(string entityName, Guid entityId,
			Relationship relationship,
			EntityReferenceCollection relatedEntities, ExecuteParams executeParams = null, CancellationToken cancellationToken = default)
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

			await TryRunOperation(
				async service =>
				{
					await service.DisassociateAsync(entityName, entityId, relationship, relatedEntities,
						cancellationToken == default ? CancellationToken.None : cancellationToken);
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
			return RetrieveAsOperationAsync(entityName, id, columnSet, executeParams, CancellationToken.None).Result;
		}

		public virtual async Task<Operation<RetrieveResponse>> RetrieveAsOperationAsync(string entityName, Guid id, ColumnSet columnSet,
			ExecuteParams executeParams = null, CancellationToken cancellationToken = default)
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
				await TryRunOperation(
					async service =>
					{
						Entity resultInner;

						if (IsCacheEnabled && executeParams?.IsCachingEnabled != false)
						{
							resultInner = (await InnerExecute<RetrieveResponse>(request)).Entity;
						}
						else
						{
							resultInner = await service.RetrieveAsync(entityName, id, columnSet,
								cancellationToken == default ? CancellationToken.None : cancellationToken);
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
			return RetrieveMultipleAsOperationAsync(query, executeParams, CancellationToken.None).Result;
		}

		public virtual async Task<Operation<RetrieveMultipleResponse>> RetrieveMultipleAsOperationAsync(QueryBase query,
			ExecuteParams executeParams = null, CancellationToken cancellationToken = default)
		{
			ValidateState();

			var request =
				new RetrieveMultipleRequest
				{
					Query = query
				};
			var operation = PrepOperation<RetrieveMultipleResponse>(request);

			var result =
				await TryRunOperation(
					async service =>
					{
						EntityCollection resultInner;

						if (IsCacheEnabled && executeParams?.IsCachingEnabled != false)
						{
							resultInner = (await InnerExecute<RetrieveMultipleResponse>(request)).EntityCollection;
						}
						else
						{
							resultInner = await service.RetrieveMultipleAsync(query,
								cancellationToken == default ? CancellationToken.None : cancellationToken);
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
			return ExecuteAsOperationAsync(request, executeParams, CancellationToken.None).Result;
		}

		public virtual async Task<Operation> ExecuteAsOperationAsync(OrganizationRequest request, ExecuteParams executeParams = null, CancellationToken cancellationToken = default)
		{
			return await ExecuteAsOperationAsync(request, executeParams, null, cancellationToken);
		}

		public virtual Operation ExecuteAsOperation(OrganizationRequest request, ExecuteParams executeParams,
			Func<IOrganizationService, OrganizationRequest, OrganizationRequest> undoFunction)
		{
			return ExecuteAsOperationAsync(request, executeParams, undoFunction, CancellationToken.None).Result;
		}

		public virtual async Task<Operation> ExecuteAsOperationAsync(OrganizationRequest request, ExecuteParams executeParams,
			Func<IOrganizationService, OrganizationRequest, OrganizationRequest> undoFunction, CancellationToken cancellationToken = default)
		{
			ValidateState();
			return await ExecuteAsOperationAsync(request, executeParams, undoFunction,
				PrepOperation<OrganizationResponse>(request) as Operation<OrganizationResponse>, cancellationToken);
		}

		public virtual Operation<TResponse> ExecuteAsOperation<TResponse>(OrganizationRequest request, ExecuteParams executeParams,
			Func<IOrganizationService, OrganizationRequest, OrganizationRequest> undoFunction)
			where TResponse : OrganizationResponse
		{
			return ExecuteAsOperationAsync<TResponse>(request, executeParams, undoFunction, CancellationToken.None).Result;
		}

		public virtual async Task<Operation<TResponse>> ExecuteAsOperationAsync<TResponse>(OrganizationRequest request, ExecuteParams executeParams,
			Func<IOrganizationService, OrganizationRequest, OrganizationRequest> undoFunction, CancellationToken cancellationToken = default)
			where TResponse : OrganizationResponse
		{
			ValidateState();
			return await ExecuteAsOperationAsync(request, executeParams, undoFunction, PrepOperation<TResponse>(request) as Operation<TResponse>, cancellationToken);
		}

		protected virtual async Task<Operation<TResponse>> ExecuteAsOperation<TResponse>(OrganizationRequest request,
			ExecuteParams executeParams,
			Func<IOrganizationService, OrganizationRequest, OrganizationRequest> undoFunction, Operation<TResponse> operation)
			where TResponse : OrganizationResponse
		{
			return ExecuteAsOperationAsync(request, executeParams, undoFunction, operation, CancellationToken.None).Result;
		}

		protected virtual async Task<Operation<TResponse>> ExecuteAsOperationAsync<TResponse>(OrganizationRequest request, ExecuteParams executeParams,
			Func<IOrganizationService, OrganizationRequest, OrganizationRequest> undoFunction, Operation<TResponse> operation, CancellationToken cancellationToken = default)
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
				await TryRunOperation(
					async service =>
						  {
						OrganizationResponse resultInner;

						if (IsCacheEnabled && executeParams?.IsCachingEnabled != false)
						{
							resultInner = await InnerExecute<OrganizationResponse>(request);
						}
						else
						{
							resultInner = await service.ExecuteAsync(request,
								cancellationToken == default ? CancellationToken.None : cancellationToken);
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
			return ExecuteDeferredRequestsAsync(bulkSize).Result;
		}

		public virtual async Task<IDictionary<OrganizationRequest, OrganisationRequestToken<OrganizationResponse>>> ExecuteDeferredRequestsAsync(
			int bulkSize = 1000)
		{
			ValidateDeferredQueueState();
			return await deferredOrgService?.ExecuteDeferredRequests(bulkSize);
		}

		public virtual void CancelDeferredRequests()
		{
			deferredOrgService?.CancelDeferredRequests();
		}

		#endregion

		#region Convenience

		public virtual UpsertResponse Upsert(Entity entity, ExecuteParams executeParams = null)
		{
			return UpsertAsync(entity, executeParams).Result;
		}

		public virtual async Task<UpsertResponse> UpsertAsync(Entity entity, ExecuteParams executeParams = null)
		{
			return await ExecuteAsync<UpsertResponse>(
				new UpsertRequest
				{
					Target = entity
				}, executeParams);
		}

		public virtual TResponse Execute<TResponse>(OrganizationRequest request, ExecuteParams executeParams = null)
			where TResponse : OrganizationResponse
		{
			return ExecuteAsync<TResponse>(request, executeParams, CancellationToken.None).Result;
		}

		public virtual async Task<TResponse> ExecuteAsync<TResponse>(OrganizationRequest request, ExecuteParams executeParams = null, CancellationToken cancellationToken = default)
			where TResponse : OrganizationResponse
		{
			return (TResponse) await ExecuteAsync(request, executeParams, null, cancellationToken);
		}

		public virtual TResponse Execute<TResponse, TRequest>(OrganizationRequest request, ExecuteParams executeParams = null,
			Func<IOrganizationService, TRequest, OrganizationRequest> undoFunction = null)
			where TResponse : OrganizationResponse
			where TRequest : OrganizationRequest
		{
			return ExecuteAsync<TResponse, TRequest>(request, executeParams, undoFunction, CancellationToken.None).Result;
		}

		public virtual async Task<TResponse> ExecuteAsync<TResponse, TRequest>(OrganizationRequest request, ExecuteParams executeParams = null,
			Func<IOrganizationService, TRequest, OrganizationRequest> undoFunction = null, CancellationToken cancellationToken = default)
			where TResponse : OrganizationResponse
			where TRequest : OrganizationRequest
		{
			return (TResponse) await ExecuteAsync(request, executeParams,
				(Func<IOrganizationService, OrganizationRequest, OrganizationRequest>)undoFunction, cancellationToken);
		}

		public virtual Operation<TResponse> ExecuteAsOperation<TResponse>(OrganizationRequest request,
			ExecuteParams executeParams = null)
			where TResponse : OrganizationResponse
		{
			return ExecuteAsOperationAsync<TResponse>(request, executeParams, CancellationToken.None).Result;
		}

		public virtual async Task<Operation<TResponse>> ExecuteAsOperationAsync<TResponse>(OrganizationRequest request,
			ExecuteParams executeParams = null, CancellationToken cancellationToken = default)
			where TResponse : OrganizationResponse
		{
			return await ExecuteAsOperationAsync<TResponse>(request, executeParams, null, cancellationToken);
		}

		public virtual Operation<TResponse> ExecuteAsOperation<TResponse, TRequest>(OrganizationRequest request,
			ExecuteParams executeParams = null, Func<IOrganizationService, TRequest, OrganizationRequest> undoFunction = null)
			where TRequest : OrganizationRequest
			where TResponse : OrganizationResponse
		{
			return ExecuteAsOperationAsync<TResponse, TRequest>(request, executeParams, undoFunction, CancellationToken.None).Result;
		}

		public virtual async Task<Operation<TResponse>> ExecuteAsOperationAsync<TResponse, TRequest>(OrganizationRequest request,
			ExecuteParams executeParams = null, Func<IOrganizationService, TRequest, OrganizationRequest> undoFunction = null, CancellationToken cancellationToken = default)
			where TRequest : OrganizationRequest
			where TResponse : OrganizationResponse
		{
			return await ExecuteAsOperationAsync(request, executeParams,
				(Func<IOrganizationService, OrganizationRequest, OrganizationRequest>)undoFunction, cancellationToken) as Operation<TResponse>;
		}

		public virtual IDictionary<OrganizationRequest, ExecuteBulkResponse> ExecuteBulk(List<OrganizationRequest> requests,
			bool isReturnResponses = false, int batchSize = 1000, bool isContinueOnError = true,
			Action<int, int, IDictionary<OrganizationRequest, ExecuteBulkResponse>> bulkFinishHandler = null,
			ExecuteParams executeParams = null)
		{
			return ExecuteBulkAsync(requests, isReturnResponses, batchSize, isContinueOnError, bulkFinishHandler, executeParams).Result;
		}

		public virtual async Task<IDictionary<OrganizationRequest, ExecuteBulkResponse>> ExecuteBulkAsync(List<OrganizationRequest> requests,
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

				var operation = await ExecuteAsOperationAsync<ExecuteMultipleResponse>(bulkRequest, executeParams);
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
			return RetrieveMultipleAsync<TEntityType>(query, limit, executeParams).Result;
		}

		public virtual async Task<IEnumerable<TEntityType>> RetrieveMultipleAsync<TEntityType>(string query, int limit = -1,
			ExecuteParams executeParams = null)
			where TEntityType : Entity
		{
			ValidateState();
			
			return await RetrieveMultipleAsync<TEntityType>(await RequestHelper.CloneQueryAsync(this, query), limit, executeParams);
		}

		public virtual IEnumerable<TEntityType> RetrieveMultiple<TEntityType>(QueryExpression query, int limit = -1,
			ExecuteParams executeParams = null)
			where TEntityType : Entity
		{
			return RetrieveMultipleAsync<TEntityType>(query, limit, executeParams).Result;
		}

		public virtual async Task<IEnumerable<TEntityType>> RetrieveMultipleAsync<TEntityType>(QueryExpression query, int limit = -1,
			ExecuteParams executeParams = null)
			where TEntityType : Entity
		{
			ValidateState();

			return await RetrieveMultipleRangePagedAsync<TEntityType>(query, 1,
				limit <= 0 ? int.MaxValue : (int)Math.Ceiling(limit / 5000d),
				limit <= 0 ? int.MaxValue : limit, executeParams);
		}

		public virtual IEnumerable<TEntityType> RetrieveMultipleRangePaged<TEntityType>(QueryExpression query,
			int pageStart = 1, int pageEnd = 1, int pageSize = 5000, ExecuteParams executeParams = null)
			where TEntityType : Entity
		{
			return RetrieveMultipleRangePagedAsync<TEntityType>(query, pageStart, pageEnd, pageSize, executeParams).Result;
		}

		public virtual async Task<IEnumerable<TEntityType>> RetrieveMultipleRangePagedAsync<TEntityType>(QueryExpression query,
			int pageStart = 1, int pageEnd = 1, int pageSize = 5000, ExecuteParams executeParams = null)
			where TEntityType : Entity
		{
			ValidateState();

			pageStart.RequireAtLeast(1, nameof(pageStart));
			pageEnd.RequireAtLeast(1, nameof(pageEnd));

			var entities = new List<TEntityType>();

			for (var i = pageStart; i <= pageEnd; i++)
			{
				var result = (await RetrieveMultiplePageAsync<TEntityType>(query, pageSize, i, executeParams))
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
			return RetrieveMultiplePageAsync<TEntityType>(query, pageSize, page, executeParams).Result;
		}

		public virtual async Task<IEnumerable<TEntityType>> RetrieveMultiplePageAsync<TEntityType>(QueryExpression query, int pageSize = 5000,
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
				query.PageInfo.PagingCookie = await RequestHelper.GetCookieAsync(this, query, pageSize, page);
			}

			var result = (await RetrieveMultipleAsOperationAsync(query, executeParams)).Response?.EntityCollection;

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
			return GetRecordsCountAsync(query).Result;
		}

		public virtual async Task<int> GetRecordsCountAsync(QueryBase query)
		{
			ValidateState();
			return await RequestHelper.GetTotalRecordsCountAsync(this, query);
		}

		public virtual int GetPagesCount(QueryBase query, int pageSize = 5000)
		{
			return GetPagesCountAsync(query, pageSize).Result;
		}

		public virtual async Task<int> GetPagesCountAsync(QueryBase query, int pageSize = 5000)
		{
			ValidateState();
			pageSize.RequireInRange(1, 5000, nameof(pageSize));
			return await RequestHelper.GetTotalPagesCountAsync(this, query, pageSize);
		}

		public virtual QueryExpression CloneQuery(QueryBase query)
		{
			return CloneQueryAsync(query).Result;
		}

		public virtual async Task<QueryExpression> CloneQueryAsync(QueryBase query)
		{
			ValidateState();
			return await RequestHelper.CloneQueryAsync(this, query);
		}

		#endregion

		#region Operation Handling

		private int x = 0;
		protected internal virtual async Task<TResult> TryRunOperation<TResult>(Func<IOrganizationServiceAsync2, Task<TResult>> action,
			Operation operation, ExecuteParams? executeParams, bool isDelegated = false)
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
					await opsSemaphore.WaitAsync();
					pendingOperations.Add(operation); 
					opsSemaphore.Release();
				}

				while (true)
				{
					var service = await GetService();
					operation.Service = service;
					
					var isDisposed = false;
					
					try
					{
						operation.OperationStatus = Status.InProgress;
						var result = await action(service);
						operation.OperationStatus = Status.RequestDone;
						return result;
					}
					catch (Exception ex)
					{
						operation.OperationStatus = Status.RequestDone;

						var isForceRetry = false;

						if (ex is FaultException<OrganizationServiceFault> svcFault)
						{
							isForceRetry = Parameters?.ConnectionParams?.IsApiLimit(svcFault.Detail.ErrorCode) is true;
							//Console.WriteLine($"EX: {svcFault.Detail.Message}");
						}

						if (!isForceRetry && (!isRetryEnabled || currentRetry >= maxRetryCount || nextInterval > maxmimumRetryInterval))
						{
							if (!isDelegated)
							{
								FailureCount++;

								operation.Exception = ex;
								service.Dispose();
								isDisposed = true;

								if (CustomRetryFunctions != null)
								{
									foreach (var function in CustomRetryFunctions)
									{
										try
										{
											var customRetryResult = await function(async s => await action(s), operation, executeParams, ex);

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

						operation.PreException = ex;
						operation.OperationStatus = Status.Retry;
						service.Dispose();
						isDisposed = true;

						await Task.Delay(nextInterval);
						nextInterval = new TimeSpan((long)(nextInterval.Ticks * backoffMultiplier));

						currentRetry++;

						if (!isDelegated)
						{
							RetryCount++;
						}
					}
					finally
					{
						if (!isDisposed)
						{
							service.Dispose();
						}
					}
				}
			}
			finally
			{
				if (!isDelegated)
				{
					RequestCount++;

					await opsSemaphore.WaitAsync();
					pendingOperations.Remove(operation);
					opsSemaphore.Release();

					if (executeParams?.IsExcludeFromHistory != true && Parameters?.OperationHistoryLimit != 0)
					{
						await opsSemaphore.WaitAsync();
						executedOperations.Enqueue(operation);
						opsSemaphore.Release();
					}
				}

				operation.ClearEventHandlers();
			}
		}

		protected virtual void OnOperationStatusChanged(OperationStatusEventArgs e, IOrganizationService s)
		{
			InnerOperationStatusChanged?.Invoke(this, e, s);
		}

		protected virtual Operation PrepOperation<TResponse>(OrganizationRequest request) where TResponse : OrganizationResponse
		{
			var operation =
				new Operation<TResponse>(request)
				{
					Index = ++OperationIndex
				};
			operation.OperationStatusChanged += (o, e, s) => OnOperationStatusChanged(e, s);
			operation.OperationStatus = Status.Ready;
			return operation;
		}

		#endregion

		public virtual void Dispose()
		{
			IsReleased = true;
			InnerOperationStatusChanged = null;
			CancelDeferredRequests();
			ReleaseService?.Invoke();
		}
	}
}
