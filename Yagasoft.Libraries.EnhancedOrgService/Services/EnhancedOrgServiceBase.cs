#region Imports

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Text.RegularExpressions;
using Yagasoft.Libraries.Common;
using Yagasoft.Libraries.EnhancedOrgService.Exceptions;
using Yagasoft.Libraries.EnhancedOrgService.Helpers;
using Yagasoft.Libraries.EnhancedOrgService.Params;
using Yagasoft.Libraries.EnhancedOrgService.Response;
using Yagasoft.Libraries.EnhancedOrgService.Transactions;
using Microsoft.Xrm.Client.Caching;
using Microsoft.Xrm.Client.Services;
using Microsoft.Xrm.Client.Services.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Services
{
	/// <summary>
	///     Author: Ahmed Elsawalhy<br />
	///     Version: 4.1.1
	/// </summary>
	public abstract class EnhancedOrgServiceBase : StateBase, IEnhancedOrgService
	{
		internal ITransactionManager TransactionManager;
		protected int OperationIndex;

		private readonly BlockingQueue<IOrganizationService> servicesQueue = new BlockingQueue<IOrganizationService>();
		internal Action ReleaseService;

		private bool IsCacheEnabled => Cache != null;
		internal IOrganizationServiceCache Cache { get; set; }
		internal ObjectCache ObjectCache { get; set; }

		protected EnhancedServiceParams Parameters;

		protected EnhancedOrgServiceBase(EnhancedServiceParams parameters)
		{
			Parameters = parameters;
		}

		#region Service

		internal void FillServicesQueue(IEnumerable<IOrganizationService> services)
		{
			foreach (var service in services)
			{
				servicesQueue.Add(service);
			}

			if (servicesQueue.Any())
			{
				IsValid = true;
			}
		}

		internal IOrganizationService[] ClearServicesQueue()
		{
			var services = servicesQueue.ToArray();
			servicesQueue.Clear();
			return services;
		}

		internal SelfEnqueuingService GetService()
		{
			return new SelfEnqueuingService(servicesQueue, servicesQueue.Dequeue());
		}

		#endregion

		#region Transactions

		private void ValidateTransactionSupport()
		{
			if (TransactionManager == null)
			{
				throw new UnsupportedException("Transactions are not enabled for this service.");
			}
		}

		public Transaction BeginTransaction(string transactionId = null)
		{
			ValidateTransactionSupport();
			ValidateState();

			return TransactionManager.BeginTransaction(transactionId);
		}

		public void UndoTransaction(Transaction transaction = null)
		{
			ValidateTransactionSupport();
			ValidateState();

			using (var service = GetService())
			{
				TransactionManager.UndoTransaction(service, transaction);
			}
		}

		public void AddUndoLogicToCache<TRequestType>(
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

		public void EndTransaction(Transaction transaction = null)
		{
			ValidateTransactionSupport();
			ValidateState();

			TransactionManager.EndTransaction(transaction);
		}

	    #endregion

		#region Caching

		private T Execute<T>(OrganizationRequest request, Func<OrganizationResponse, T> selector, string selectorCacheKey)
		{
			var execute = Cache == null
				? null
				: new Func
					<OrganizationRequest, Func<OrganizationRequest, OrganizationResponse>, Func<OrganizationResponse, T>, string, T>
					(Cache.Execute);

			return Execute(request, execute, selector, selectorCacheKey);
		}

		private T Execute<T>(OrganizationRequest request) where T : OrganizationResponse
		{
			return Execute(request, response => response as T, null);
		}

		private T Execute<T>(OrganizationRequest request,
			Func<OrganizationRequest, Func<OrganizationRequest, OrganizationResponse>, Func<OrganizationResponse, T>, string, T>
				execute,
			Func<OrganizationResponse, T> selector, string selectorCacheKey)
		{
			return execute == null ? selector(InnerExecute(request)) : execute(request, InnerExecute, selector, selectorCacheKey);
		}

		private OrganizationResponse InnerExecute(OrganizationRequest request)
		{
			using (var service = GetService())
			{
				return service.Execute(request is KeyedRequest keyedRequest ? keyedRequest.Request : request);
			}
		}

	    public void RemoveFromCache(Entity record)
	    {
	        Cache.Remove(record);
	    }

	    public void RemoveFromCache(EntityReference entity)
	    {
	        Cache.Remove(entity);
	    }

        public void RemoveFromCache(string entityLogicalName, Guid? id)
	    {
            Cache.Remove(entityLogicalName, id);
	    }

        public void RemoveFromCache(OrganizationRequest request)
	    {
            Cache.Remove(request);
	    }

        public void RemoveAllFromCache()
	    {
            Cache.Remove(new OrganizationServiceCachePluginMessage {Category = CacheItemCategory.All});
	    }

        /// <inheritdoc />
        public void ClearCache()
		{
			if (ObjectCache == null)
			{
				throw new UnsupportedException("The query's memory cache is not limited to this service's scope."
					+ " Use the factory's method to clear the cache instead.");
			}

			ObjectCache.Clear();
		}

		#endregion

		#region Sync

		public Guid Create(Entity entity)
		{
			ValidateState();

			var request = new CreateRequest
						  {
							  Target = entity
						  };
			var operation = new Operation<Guid>(request)
							{
								Index = OperationIndex++
							};

			using (var service = GetService())
			{
				if (TransactionManager?.IsTransactionInEffect() == true)
				{
					TransactionManager.ProcessRequest(service, operation);
				}

				try
				{
					var id = operation.Response = service.Create(entity);

					if (IsCacheEnabled)
					{
						Cache.Remove(entity);
					}

					return id;
				}
				catch (Exception ex)
				{
					operation.Exception = ex;
					throw;
				}
			}
		}

		public void Update(Entity entity)
		{
			ValidateState();

			var request = new UpdateRequest
						  {
							  Target = entity
						  };
			var operation = new Operation<object>(request)
							{
								Index = OperationIndex++
							};

			using (var service = GetService())
			{
				if (TransactionManager?.IsTransactionInEffect() == true)
				{
					TransactionManager.ProcessRequest(service, operation);
				}

				try
				{
					service.Update(entity);
					operation.Response = new object();

					if (IsCacheEnabled)
					{
						Cache.Remove(entity);
					}
				}
				catch (Exception ex)
				{
					operation.Exception = ex;
					throw;
				}
			}
		}

		public void Delete(string entityName, Guid id)
		{
			ValidateState();

			var request = new DeleteRequest
						  {
							  Target = new EntityReference(entityName, id)
						  };
			var operation = new Operation<object>(request)
							{
								Index = OperationIndex++
							};

			using (var service = GetService())
			{
				if (TransactionManager?.IsTransactionInEffect() == true)
				{
					TransactionManager.ProcessRequest(service, operation);
				}

				try
				{
					service.Delete(entityName, id);
					operation.Response = new object();

					if (IsCacheEnabled)
					{
						Cache.Remove(entityName, id);
					}
				}
				catch (Exception ex)
				{
					operation.Exception = ex;
					throw;
				}
			}
		}

		public void Associate(string entityName, Guid entityId, Relationship relationship,
			EntityReferenceCollection relatedEntities)
		{
			ValidateState();

			var request = new AssociateRequest
						  {
							  Target = new EntityReference(entityName, entityId),
							  Relationship = relationship,
							  RelatedEntities = relatedEntities
						  };
			var operation = new Operation<object>(request)
							{
								Index = OperationIndex++
							};

			using (var service = GetService())
			{
				try
				{
					service.Associate(entityName, entityId, relationship, relatedEntities);
				}
				catch (Exception ex)
				{
					operation.Exception = ex;
					throw;
				}

				if (TransactionManager?.IsTransactionInEffect() == true)
				{
					TransactionManager.ProcessRequest(service, operation);
				}

				operation.Response = new object();
			}
		}

		public void Disassociate(string entityName, Guid entityId, Relationship relationship,
			EntityReferenceCollection relatedEntities)
		{
			ValidateState();

			var request = new DisassociateRequest
						  {
							  Target = new EntityReference(entityName, entityId),
							  Relationship = relationship,
							  RelatedEntities = relatedEntities
						  };
			var operation = new Operation<object>(request)
							{
								Index = OperationIndex++
							};

			using (var service = GetService())
			{
				try
				{
					service.Disassociate(entityName, entityId, relationship, relatedEntities);
				}
				catch (Exception ex)
				{
					operation.Exception = ex;
					throw;
				}

				if (TransactionManager?.IsTransactionInEffect() == true)
				{
					TransactionManager.ProcessRequest(service, operation);
				}

				operation.Response = new object();
			}
		}

		public Entity Retrieve(string entityName, Guid id, ColumnSet columnSet)
		{
			ValidateState();

			if (IsCacheEnabled)
			{
				return Execute<RetrieveResponse>(
					new RetrieveRequest
					{
						Target = new EntityReference
								 {
									 LogicalName = entityName,
									 Id = id
								 },
						ColumnSet = columnSet
					})?.Entity;
			}

			using (var service = GetService())
			{
				return service.Retrieve(entityName, id, columnSet);
			}
		}

		public EntityCollection RetrieveMultiple(QueryBase query)
		{
			ValidateState();

			if (IsCacheEnabled)
			{
				return Execute<RetrieveMultipleResponse>(
					new RetrieveMultipleRequest
					{
						Query = query
					})?.EntityCollection;
			}

			using (var service = GetService())
			{
				return service.RetrieveMultiple(query);
			}
		}

		public OrganizationResponse Execute(OrganizationRequest request)
		{
			return Execute(request, null);
		}

		public virtual OrganizationResponse Execute(OrganizationRequest request,
			Func<IOrganizationService, OrganizationRequest, OrganizationRequest> undoFunction)
		{
			ValidateState();

			var operation = new Operation<OrganizationResponse>(request)
							{
								Index = OperationIndex++
							};

			try
			{
				using (var service = GetService())
				{
					// add to transaction if there is one
					if (TransactionManager?.IsTransactionInEffect() == true)
					{
						TransactionManager.ProcessRequest(service, operation, undoFunction);
					}

					if (!IsCacheEnabled)
					{
						return operation.Response = service.Execute(request);
					}
				}

				return operation.Response = Execute<OrganizationResponse>(request);
			}
			catch (Exception ex)
			{
				operation.Exception = ex;
				throw;
			}
		}

		#endregion

		#region Convenience

		public Dictionary<OrganizationRequest, ExecuteBulkResponse> ExecuteBulk(List<OrganizationRequest> requests,
			bool isReturnResponses = false, int batchSize = 1000, bool isContinueOnError = true,
			Action<int, int, IDictionary<OrganizationRequest, ExecuteBulkResponse>> bulkFinishHandler = null)
		{
			ValidateState();

			batchSize.RequireAbove(0, "requests");
			batchSize.RequireInRange(1, 1000, "bulkSize");

			var bulkRequest = new ExecuteMultipleRequest
							  {
								  Requests = new OrganizationRequestCollection(),
								  Settings = new ExecuteMultipleSettings
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

				ExecuteMultipleResponse bulkResponses;

				var service = servicesQueue.Dequeue();

				var operation = new Operation<OrganizationResponse>(bulkRequest)
								{
									Index = OperationIndex++
								};

				// add to transaction if there is one
				if (TransactionManager?.IsTransactionInEffect() == true)
				{
					TransactionManager.ProcessRequest(service, operation);
				}

				if (IsCacheEnabled)
				{
					operation.Response = bulkResponses = (ExecuteMultipleResponse)Execute(bulkRequest);
				}
				else
				{
					operation.Response = bulkResponses = (ExecuteMultipleResponse)service.Execute(bulkRequest);
					servicesQueue.Enqueue(service);
					service = null;
				}

				var innerRequests = bulkRequest.Requests;

				bulkRequest = new ExecuteMultipleRequest
							  {
								  Requests = new OrganizationRequestCollection(),
								  Settings = new ExecuteMultipleSettings
											 {
												 ContinueOnError = isContinueOnError,
												 ReturnResponses = isReturnResponses
											 }
							  };

				// no need to build a response
				if (!isReturnResponses)
				{
					// break on error and no 'continue on error' option
					if (!isContinueOnError && (bulkResponses.IsFaulted || bulkResponses.Responses.Any(e => e.Fault != null)))
					{
						break;
					}
					else
					{
						bulkFinishHandler?.Invoke(i + 1, (int)batchCount, perBulkResponses);
						continue;
					}
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

					var response = new ExecuteBulkResponse
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

		public List<TEntityType> RetrieveMultiple<TEntityType>(QueryExpression query, int limit = -1)
			where TEntityType : Entity
		{
			ValidateState();

			return RetrieveMultipleRangePaged<TEntityType>(query, 1,
				limit <= 0 ? int.MaxValue : (int)Math.Ceiling(limit / 5000d),
				limit <= 0 ? int.MaxValue : 5000);
		}

		public List<TEntityType> RetrieveMultipleRangePaged<TEntityType>(QueryExpression query,
			int pageStart = 1, int pageEnd = 1, int pageSize = 5000)
			where TEntityType : Entity
		{
			ValidateState();

			pageStart.RequireAtLeast(1, nameof(pageStart));
			pageEnd.RequireAtLeast(1, nameof(pageEnd));

			var entities = new List<TEntityType>();

			for (var i = pageStart; i <= pageEnd; i++)
			{
				var result = RetrieveMultiplePage<TEntityType>(query, pageSize, i);
				entities.AddRange(result);

				if (!result.Any())
				{
					break;
				}
			}

			return entities;
		}

		public List<TEntityType> RetrieveMultiplePage<TEntityType>(QueryExpression query, int pageSize = 5000, int page = 1)
			where TEntityType : Entity
		{
			ValidateState();

			page.RequireAtLeast(1, nameof(page));
			page.RequireInRange(0, 5000, nameof(pageSize));

			EntityCollection result;

			using (var service = GetService())
			{
				query.PageInfo = query.PageInfo ?? new PagingInfo();
				query.PageInfo.Count = pageSize;
				query.PageInfo.PageNumber = page;

				var currentCookie = query.PageInfo.PagingCookie;

				if (page > 1
					&& (currentCookie == null
						|| page - 1 != int.Parse(Regex.Match(currentCookie, @"page=""(.*?)""").Groups[1].ToString())))
				{
					query.PageInfo.PagingCookie = RequestHelper.GetCookie(service, query, pageSize, page);
				}

				if (!IsCacheEnabled)
				{
					result = service.RetrieveMultiple(query);
					query.PageInfo.PagingCookie = result.PagingCookie;
					return result.Entities.Select(entity => entity.ToEntity<TEntityType>()).ToList();
				}
			}

			result = RetrieveMultiple(query);
			query.PageInfo.PagingCookie = result.PagingCookie;
			return result.Entities.Select(entity => entity.ToEntity<TEntityType>()).ToList();
		}

		#endregion

		#endregion

		#region Utility

		public int GetRecordsCount(QueryBase query)
		{
			ValidateState();

			using (var service = GetService())
			{
				return RequestHelper.GetTotalRecordsCount(service, query);
			}
		}

		public int GetPagesCount(QueryBase query, int pageSize = 5000)
		{
			ValidateState();

			using (var service = GetService())
			{
				pageSize.RequireInRange(1, 5000, nameof(pageSize));
				return RequestHelper.GetTotalPagesCount(service, query, pageSize);
			}
		}

		public QueryExpression CloneQuery(QueryBase query)
		{
			ValidateState();

			using (var service = GetService())
			{
				return RequestHelper.CloneQuery(service, query);
			}
		}

		#endregion

		public void Dispose()
		{
			ReleaseService?.Invoke();
			IsValid = false;
		}
	}
}
