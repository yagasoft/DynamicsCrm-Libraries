#region Imports

using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Yagasoft.Libraries.Common;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced.Features;
using Yagasoft.Libraries.EnhancedOrgService.Transactions;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced
{
	public interface IEnhancedOrgService : IOrganizationService, IDisposable
	{
		/// <summary>
		///     Starts a new transaction. After calling this method, all service requests can be undone by calling
		///     <see cref="EnhancedOrgServiceBase.UndoTransaction" />.
		/// </summary>
		/// <param name="transactionId">[OPTIONAL] The transaction ID.</param>
		/// <returns>A new transaction object to use when reverting the transaction.</returns>
		Transaction BeginTransaction(string transactionId = null);

		/// <summary>
		///     Reverts the transaction, which executes 'undo' requests for every service request made since the start of this
		///     transaction.
		///     If no transaction is given, it reverts ALL transactions.
		/// </summary>
		/// <param name="transaction">[OPTIONAL] The transaction to revert.</param>
		void UndoTransaction(Transaction transaction = null);

		/// <summary>
		///     Adds undo logic for the request type given to the cache.
		/// </summary>
		/// <typeparam name="TRequestType">The type of the request to undo.</typeparam>
		/// <param name="undoFunction">The undo function, which takes the original request and returns the undo request.</param>
		void AddUndoLogicToCache<TRequestType>(
			Func<IOrganizationService, OrganizationRequest, OrganizationRequest> undoFunction)
			where TRequestType : OrganizationRequest;

		/// <summary>
		///     Ends the transaction, which excludes requests from future reverts; however, nested transactions can still be
		///     reverted!
		/// </summary>
		/// <param name="transaction">The transaction to end.</param>
		void EndTransaction(Transaction transaction = null);

		/// <summary>
		///     Removed an entity from cache.<br />
		/// </summary>
		void RemoveFromCache(Entity record);

		/// <summary>
		///     Removed an entity from cache.<br />
		/// </summary>
		void RemoveFromCache(EntityReference entity);

		/// <summary>
		///     Removed an entity from cache.<br />
		/// </summary>
		void RemoveFromCache(string entityLogicalName, Guid? id);

		/// <summary>
		///     Removed based on request from cache.<br />
		/// </summary>
		void RemoveFromCache(OrganizationRequest request);

		/// <summary>
		///     Removed all entities from cache.<br />
		/// </summary>
		void RemoveAllFromCache();

		/// <summary>
		///     Clears the query's memory cache.<br />
		///     If the cache is not only scoped to this service (factory's 'PrivatePerInstance' setting), an exception is thrown.
		/// </summary>
		void ClearCache();

		/// <summary>
		///     Calls <see cref="Entity.ToEntity{T}" /> after the retrieve operation.
		///     <seealso cref="IOrganizationService.Retrieve" />
		/// </summary>
		/// <typeparam name="TEntity">Returned entity type.</typeparam>
		public TEntity Retrieve<TEntity>(string entityName, Guid id, ColumnSet columnSet) where TEntity : Entity;

		/// <summary>
		///     Executes the specified request with support for reversion.
		/// </summary>
		/// <param name="request">The request to execute.</param>
		/// <param name="undoFunction">The undo function to be used as an instruction on how to undo the given request.</param>
		/// <returns>An organisation response.</returns>
		OrganizationResponse Execute(OrganizationRequest request,
			Func<IOrganizationService, OrganizationRequest, OrganizationRequest> undoFunction);

		/// <summary>
		///     Casts the response after calling <see cref="Execute" />.
		///     <seealso cref="Execute" />
		/// </summary>
		/// <param name="request">The request to execute.</param>
		/// <param name="undoFunction">The undo function to be used as an instruction on how to undo the given request.</param>
		/// <returns>An typed organisation response.</returns>
		TResponse Execute<TResponse, TRequest>(TRequest request,
			Func<IOrganizationService, TRequest, OrganizationRequest> undoFunction = null)
			where TRequest : OrganizationRequest
			where TResponse : OrganizationResponse;

		/// <summary>
		///     Upsert a record.
		/// </summary>
		UpsertResponse Upsert(Entity entity);

		/// <summary>
		///     Executes given requests in bulk. The returned value should only be taken into consideration
		///     if 'isReturnResponses' is 'true'.<br />
		/// </summary>
		/// <param name="requests">The requests queue.</param>
		/// <param name="isReturnResponses">[OPTIONAL] This must be true to get the list of errors that occur in CRM.</param>
		/// <param name="batchSize">     [OPTIONAL] How many requests to bundle per execution. </param>
		/// <param name="isContinueOnError">
		///     [OPTIONAL] Ignore errors in CRM, but will still return them if 'isReturnResponses' is true.
		/// </param>
		/// <param name="bulkFinishHandler">
		///     [OPTIONAL] Handler called every time a batch is done.
		///     The handler takes 'current batch index (1, 2 ... etc.), total batch count, responses' as parameters.
		/// </param>
		/// <returns>A queue of responses to each request, in order.</returns>
		/// <exception cref="System.Exception">Exception thrown if the execution fails.</exception>
		IDictionary<OrganizationRequest, ExecuteBulkResponse> ExecuteBulk(List<OrganizationRequest> requests,
			bool isReturnResponses = false, int batchSize = 1000, bool isContinueOnError = true,
			Action<int, int, IDictionary<OrganizationRequest, ExecuteBulkResponse>> bulkFinishHandler = null);

		/// <summary>
		///     This is a convenience method that takes a query, and retrieves records.<br />
		///     The query is done in parallel if paging is required, with max concurrency as the max connections set in this
		///     object.
		/// </summary>
		/// <typeparam name="TEntityType">The type of the entities returned (pass 'Entity' if not using early-bound).</typeparam>
		/// <param name="query">The query.</param>
		/// <param name="limit">[OPTIONAL] How many entities to retrieve. </param>
		/// <returns>A list of entities fitting the query conditions and cast to the type passed.</returns>
		IEnumerable<TEntityType> RetrieveMultiple<TEntityType>(QueryExpression query, int limit = -1)
			where TEntityType : Entity;

		/// <summary>
		///     This is a convenience method that takes a query, and retrieves records. It takes into account that paging has a
		///     limit, and returns ALL records fitting the query conditions.<br />
		///     If the page is not specified, first page will be retrieved.
		///     The query is done in parallel if paging is required, with max concurrency as the max connections set in this
		///     object.
		/// </summary>
		/// <typeparam name="TEntityType">The type of the entities returned (pass 'Entity' if not using early-bound).</typeparam>
		/// <param name="query">The query.</param>
		/// <param name="pageStart">[OPTIONAL] If specified, first page is set, otherwise, first page.</param>
		/// <param name="pageEnd">
		///     [OPTIONAL] If specified, last page is set, otherwise, first page -- effectively retrieving one
		///     page.
		/// </param>
		/// <param name="pageSize">[OPTIONAL] How many entities to retrieve per page. </param>
		/// <returns>A list of entities fitting the query conditions and cast to the type passed.</returns>
		IEnumerable<TEntityType> RetrieveMultipleRangePaged<TEntityType>(QueryExpression query,
			int pageStart = 1, int pageEnd = 1, int pageSize = 5000)
			where TEntityType : Entity;

		/// <summary>
		///     This is a convenience method that takes a query, and retrieves records. It takes into account that paging has a
		///     limit, and returns ALL records fitting the query conditions.<br />
		///     If the page is not specified, first page will be retrieved.
		/// </summary>
		/// <typeparam name="TEntityType">The type of the entities returned (pass 'Entity' if not using early-bound).</typeparam>
		/// <param name="query">The query.</param>
		/// <param name="pageSize">[OPTIONAL] How many entities to retrieve per page. </param>
		/// <param name="page">[OPTIONAL] If specified, only the that page is returned, otherwise, first page.</param>
		/// <returns>A list of entities fitting the query conditions and cast to the type passed.</returns>
		IEnumerable<TEntityType> RetrieveMultiplePage<TEntityType>(QueryExpression query, int pageSize = 5000, int page = 1)
			where TEntityType : Entity;

		/// <summary>
		///     Gets the total number of records for this query.
		/// </summary>
		/// <param name="query">The query.</param>
		/// <returns>The total number of records</returns>
		int GetRecordsCount(QueryBase query);

		/// <summary>
		///     Gets the total number of pages for this query.
		/// </summary>
		/// <param name="query">The query.</param>
		/// <param name="pageSize">The page size</param>
		/// <returns>The total number of pages</returns>
		int GetPagesCount(QueryBase query, int pageSize = 5000);

		/// <summary>
		///     Clones the query.
		/// </summary>
		/// <param name="query">The query.</param>
		QueryExpression CloneQuery(QueryBase query);

		/// <summary>
		///     Returns an object that manages the deferred execution operations (single object per Org Service).
		/// </summary>
		IDeferredOrgService StartDeferredQueue();

		/// <summary>
		///     TODO
		/// </summary>
		IPlannedOrgService StartExecutionPlanning();
	}
}
