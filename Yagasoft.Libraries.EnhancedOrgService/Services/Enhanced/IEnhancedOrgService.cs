#region Imports

using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Yagasoft.Libraries.Common;
using Yagasoft.Libraries.EnhancedOrgService.Factories;
using Yagasoft.Libraries.EnhancedOrgService.Params;
using Yagasoft.Libraries.EnhancedOrgService.Pools;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced.Deferred;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced.Planned;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced.Transactions;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced
{
	/// <summary>
	///     Dynamics CRM Enhanced Organisation Service is an extension to the out-of-the-box IOrganizationService. It supports
	///     pooling, async operations, dependencies, transactions, deferred execution, and caching.<br />
	///     Use one of the following methods to create a service:<br />
	///     <c>    </c>1- Helpers: invoke one of the helpers in <see cref="Helpers.EnhancedServiceHelper" /><br />
	///     <c>    </c>2- Manual:<br />
	///     <c>        </c>a) Create a factory: create an object of
	///     <see cref="EnhancedServiceFactory{TServiceInterface,TEnhancedOrgService}" />,
	///     passing <see cref="EnhancedServiceParams" /> as a parameter to the constructor<br />
	///     <c>        </c>b) Optionally, pool services created by the factory: create a pool object of
	///     <see cref="EnhancedServicePool{TServiceInterface,TEnhancedOrgService}" />,
	///     passing the factory as a parameter to the constructor<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	public interface IEnhancedOrgService : IOrganizationService, IDisposable, ITransactionOrgService
	{
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
		///     Returns an object that manages the deferred execution operations (single object per Org Service).
		/// </summary>
		IPlannedOrgService StartExecutionPlanning();
	}
}
