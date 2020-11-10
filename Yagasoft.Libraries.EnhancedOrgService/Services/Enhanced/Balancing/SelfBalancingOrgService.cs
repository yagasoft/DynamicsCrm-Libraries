#region Imports

using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Yagasoft.Libraries.Common;
using Yagasoft.Libraries.EnhancedOrgService.Events;
using Yagasoft.Libraries.EnhancedOrgService.Events.EventArgs;
using Yagasoft.Libraries.EnhancedOrgService.Params;
using Yagasoft.Libraries.EnhancedOrgService.Response.Operations;
using Yagasoft.Libraries.EnhancedOrgService.Router;
using Yagasoft.Libraries.EnhancedOrgService.Services.SelfDisposing;
using Yagasoft.Libraries.EnhancedOrgService.Transactions;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced.Balancing
{
	public class SelfBalancingOrgService : EnhancedOrgServiceBase, ISelfBalancingOrgService
	{
		public override event EventHandler<IEnhancedOrgService, OperationStatusEventArgs> OperationStatusChanged
		{
			add => RoutingService.Stats.OperationStatusChanged += value;
			remove => RoutingService.Stats.OperationStatusChanged -= value;
		}

		public override int RequestCount => (RoutingService as RoutingService)?.Stats.RequestCount ?? -1;

		public override int FailureCount => (RoutingService as RoutingService)?.Stats.FailureCount ?? -1;

		public override double FailureRate => (RoutingService as RoutingService)?.Stats.FailureRate ?? -1;

		public override int RetryCount => (RoutingService as RoutingService)?.Stats.RetryCount ?? -1;

		public override IEnumerable<Operation> PendingOperations => (RoutingService as RoutingService)?.Stats.PendingOperations
			?? Array.Empty<Operation>();

		public override IEnumerable<Operation> ExecutedOperations => (RoutingService as RoutingService)?.Stats.ExecutedOperations
			?? Array.Empty<Operation>();

		public override IEnumerable<OrganizationRequest> DeferredRequests => throw new NotSupportedException(NotSupportedOperation);

		protected internal IRoutingService RoutingService;

		protected const string NotSupportedOperation = "Operation not supported by this service.";

		protected internal SelfBalancingOrgService(IRoutingService routingService) : base(null)
		{
			routingService.Require(nameof(routingService));
			RoutingService = routingService;
		}

		protected internal override IDisposableService GetService()
		{
			return RoutingService.GetService();
		}

		public override Transaction BeginTransaction(string transactionId = null)
		{
			throw new NotSupportedException(NotSupportedOperation);
		}

		public override void UndoTransaction(Transaction transaction = null)
		{
			throw new NotSupportedException(NotSupportedOperation);
		}

		public override void AddUndoLogicToCache<TRequestType>(
			Func<IOrganizationService, OrganizationRequest, OrganizationRequest> undoFunction)
		{
			throw new NotSupportedException(NotSupportedOperation);
		}

		public override void EndTransaction(Transaction transaction = null)
		{
			throw new NotSupportedException(NotSupportedOperation);
		}

		public override Guid Create(Entity entity)
		{
			using var service = GetService();
			return ((IEnhancedOrgService)service).Create(entity);
		}

		public override Entity Retrieve(string entityName, Guid id, ColumnSet columnSet)
		{
			using var service = GetService();
			return ((IEnhancedOrgService)service).Retrieve(entityName, id, columnSet);
		}

		public override void Update(Entity entity)
		{
			using var service = GetService();
			((IEnhancedOrgService)service).Update(entity);
		}

		public override void Delete(string entityName, Guid id)
		{
			using var service = GetService();
			((IEnhancedOrgService)service).Delete(entityName, id);
		}

		public override OrganizationResponse Execute(OrganizationRequest request)
		{
			using var service = GetService();
			return ((IEnhancedOrgService)service).Execute(request);
		}

		public override void Associate(string entityName, Guid entityId, Relationship relationship,
			EntityReferenceCollection relatedEntities)
		{
			using var service = GetService();
			((IEnhancedOrgService)service).Associate(entityName, entityId, relationship, relatedEntities);
		}

		public override void Disassociate(string entityName, Guid entityId, Relationship relationship,
			EntityReferenceCollection relatedEntities)
		{
			using var service = GetService();
			((IEnhancedOrgService)service).Disassociate(entityName, entityId, relationship, relatedEntities);
		}

		public override EntityCollection RetrieveMultiple(QueryBase query)
		{
			using var service = GetService();
			return ((IEnhancedOrgService)service).RetrieveMultiple(query);
		}

		public override UpsertResponse Upsert(Entity entity, ExecuteParams executeParams = null)
		{
			using var service = GetService();
			return ((IEnhancedOrgService)service).Upsert(entity, executeParams);
		}

		public override IDictionary<OrganizationRequest, ExecuteBulkResponse> ExecuteBulk(List<OrganizationRequest> requests,
			bool isReturnResponses = false, int batchSize = 1000, bool isContinueOnError = true,
			Action<int, int, IDictionary<OrganizationRequest, ExecuteBulkResponse>> bulkFinishHandler = null,
			ExecuteParams executeParams = null)
		{
			using var service = GetService();
			return ((IEnhancedOrgService)service).ExecuteBulk(requests, isReturnResponses, batchSize, isContinueOnError, bulkFinishHandler,
				executeParams);
		}

		public override IEnumerable<TEntityType> RetrieveMultiple<TEntityType>(QueryExpression query, int limit = -1,
			ExecuteParams executeParams = null)
		{
			using var service = GetService();
			return ((IEnhancedOrgService)service).RetrieveMultiple<TEntityType>(query, limit, executeParams);
		}

		public override IEnumerable<TEntityType> RetrieveMultipleRangePaged<TEntityType>(QueryExpression query, int pageStart = 1,
			int pageEnd = 1, int pageSize = 5000,
			ExecuteParams executeParams = null)
		{
			using var service = GetService();
			return ((IEnhancedOrgService)service).RetrieveMultipleRangePaged<TEntityType>(query, pageStart, pageEnd, pageSize,
				executeParams);
		}

		public override IEnumerable<TEntityType> RetrieveMultiplePage<TEntityType>(QueryExpression query, int pageSize = 5000,
			int page = 1,
			ExecuteParams executeParams = null)
		{
			using var service = GetService();
			return ((IEnhancedOrgService)service).RetrieveMultiplePage<TEntityType>(query, pageSize, page, executeParams);
		}

		public override void StartSdkOpDeferredQueue()
		{
			throw new NotSupportedException(NotSupportedOperation);
		}

		public override Guid Create(Entity entity, ExecuteParams executeParams)
		{
			using var service = GetService();
			return ((IEnhancedOrgService)service).Create(entity, executeParams);
		}

		public override Operation<UpdateResponse> Update(Entity entity, ExecuteParams executeParams)
		{
			using var service = GetService();
			return ((IEnhancedOrgService)service).Update(entity, executeParams);
		}

		public override Operation<DeleteResponse> Delete(string entityName, Guid id, ExecuteParams executeParams)
		{
			using var service = GetService();
			return ((IEnhancedOrgService)service).Delete(entityName, id, executeParams);
		}

		public override Operation<AssociateResponse> Associate(string entityName, Guid entityId, Relationship relationship,
			EntityReferenceCollection relatedEntities,
			ExecuteParams executeParams)
		{
			using var service = GetService();
			return ((IEnhancedOrgService)service).Associate(entityName, entityId, relationship, relatedEntities, executeParams);
		}

		public override Operation<DisassociateResponse> Disassociate(string entityName, Guid entityId, Relationship relationship,
			EntityReferenceCollection relatedEntities,
			ExecuteParams executeParams)
		{
			using var service = GetService();
			return ((IEnhancedOrgService)service).Disassociate(entityName, entityId, relationship, relatedEntities, executeParams);
		}

		public override Entity Retrieve(string entityName, Guid id, ColumnSet columnSet, ExecuteParams executeParams)
		{
			using var service = GetService();
			return ((IEnhancedOrgService)service).Retrieve(entityName, id, columnSet, executeParams);
		}

		public override TEntity Retrieve<TEntity>(string entityName, Guid id, ColumnSet columnSet, ExecuteParams executeParams)
		{
			using var service = GetService();
			return ((IEnhancedOrgService)service).Retrieve<TEntity>(entityName, id, columnSet, executeParams);
		}

		public override EntityCollection RetrieveMultiple(QueryBase query, ExecuteParams executeParams)
		{
			using var service = GetService();
			return ((IEnhancedOrgService)service).RetrieveMultiple(query, executeParams);
		}

		public override OrganizationResponse Execute(OrganizationRequest request, ExecuteParams executeParams)
		{
			using var service = GetService();
			return ((IEnhancedOrgService)service).Execute(request, executeParams);
		}

		public override OrganizationResponse Execute(OrganizationRequest request, ExecuteParams executeParams,
			Func<IOrganizationService, OrganizationRequest, OrganizationRequest> undoFunction)
		{
			using var service = GetService();
			return ((IEnhancedOrgService)service).Execute(request, executeParams, undoFunction);
		}

		public override TResponse Execute<TResponse>(OrganizationRequest request, ExecuteParams executeParams = null)
		{
			using var service = GetService();
			return ((IEnhancedOrgService)service).Execute<TResponse>(request, executeParams);
		}

		public override TResponse Execute<TResponse, TRequest>(OrganizationRequest request, ExecuteParams executeParams = null,
			Func<IOrganizationService, TRequest, OrganizationRequest> undoFunction = null)
		{
			using var service = GetService();
			return ((IEnhancedOrgService)service).Execute<TResponse, TRequest>(request, executeParams, undoFunction);
		}

		public override Operation<CreateResponse> CreateAsOperation(Entity entity, ExecuteParams executeParams = null)
		{
			using var service = GetService();
			return ((IEnhancedOrgService)service).CreateAsOperation(entity, executeParams);
		}

		public override Operation<UpdateResponse> UpdateAsOperation(Entity entity, ExecuteParams executeParams = null)
		{
			using var service = GetService();
			return ((IEnhancedOrgService)service).UpdateAsOperation(entity, executeParams);
		}

		public override Operation<DeleteResponse> DeleteAsOperation(string entityName, Guid id, ExecuteParams executeParams = null)
		{
			using var service = GetService();
			return ((IEnhancedOrgService)service).DeleteAsOperation(entityName, id, executeParams);
		}

		public override Operation<AssociateResponse> AssociateAsOperation(string entityName, Guid entityId, Relationship relationship,
			EntityReferenceCollection relatedEntities, ExecuteParams executeParams = null)
		{
			using var service = GetService();
			return ((IEnhancedOrgService)service).AssociateAsOperation(entityName, entityId, relationship, relatedEntities, executeParams);
		}

		public override Operation<DisassociateResponse> DisassociateAsOperation(string entityName, Guid entityId,
			Relationship relationship,
			EntityReferenceCollection relatedEntities, ExecuteParams executeParams = null)
		{
			using var service = GetService();
			return ((IEnhancedOrgService)service).DisassociateAsOperation(entityName, entityId, relationship, relatedEntities,
				executeParams);
		}

		public override Operation<RetrieveResponse> RetrieveAsOperation(string entityName, Guid id, ColumnSet columnSet,
			ExecuteParams executeParams = null)
		{
			using var service = GetService();
			return ((IEnhancedOrgService)service).RetrieveAsOperation(entityName, id, columnSet, executeParams);
		}

		public override Operation<RetrieveMultipleResponse> RetrieveMultipleAsOperation(QueryBase query,
			ExecuteParams executeParams = null)
		{
			using var service = GetService();
			return ((IEnhancedOrgService)service).RetrieveMultipleAsOperation(query, executeParams);
		}

		public override Operation ExecuteAsOperation(OrganizationRequest request, ExecuteParams executeParams = null)
		{
			using var service = GetService();
			return ((IEnhancedOrgService)service).ExecuteAsOperation(request, executeParams);
		}

		public override Operation ExecuteAsOperation(OrganizationRequest request, ExecuteParams executeParams,
			Func<IOrganizationService, OrganizationRequest, OrganizationRequest> undoFunction)
		{
			using var service = GetService();
			return ((IEnhancedOrgService)service).ExecuteAsOperation(request, executeParams, undoFunction);
		}

		public override Operation<TResponse> ExecuteAsOperation<TResponse>(OrganizationRequest request,
			ExecuteParams executeParams = null)
		{
			using var service = GetService();
			return ((IEnhancedOrgService)service).ExecuteAsOperation<TResponse>(request, executeParams);
		}

		public override Operation<TResponse> ExecuteAsOperation<TResponse, TRequest>(OrganizationRequest request,
			ExecuteParams executeParams = null,
			Func<IOrganizationService, TRequest, OrganizationRequest> undoFunction = null)
		{
			using var service = GetService();
			return ((IEnhancedOrgService)service).ExecuteAsOperation<TResponse, TRequest>(request, executeParams, undoFunction);
		}

		public override int GetRecordsCount(QueryBase query)
		{
			using var service = GetService();
			return ((IEnhancedOrgService)service).GetRecordsCount(query);
		}

		public override int GetPagesCount(QueryBase query, int pageSize = 5000)
		{
			using var service = GetService();
			return ((IEnhancedOrgService)service).GetPagesCount(query, pageSize);
		}

		public override QueryExpression CloneQuery(QueryBase query)
		{
			using var service = GetService();
			return ((IEnhancedOrgService)service).CloneQuery(query);
		}

		public override void Dispose()
		{
			RoutingService.StopRouter();
		}
	}
}
