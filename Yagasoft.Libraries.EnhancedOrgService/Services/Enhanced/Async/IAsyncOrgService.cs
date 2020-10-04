#region Imports

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Xrm.Client.Services;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Yagasoft.Libraries.Common;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced.Transactions;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced.Async
{
	public interface IAsyncOrgService : IEnhancedOrgService
	{
		/// <summary>
		///     Run <see cref="OrganizationService.Create" /> asynchronously, and return a reponse object that contains the result.
		///     Accessing the result will block the thread until the process finishes communicating with CRM.
		/// </summary>
		Task<Guid> CreateAsync(Entity entity, params Task[] dependencies);

		/// <summary>
		///     Run <see cref="OrganizationService.Retrieve" /> asynchronously, and return a reponse object that contains the
		///     result.
		///     Accessing the result will block the thread until the process finishes communicating with CRM.
		/// </summary>
		Task<Entity> RetrieveAsync(string entityName, Guid id, ColumnSet columnSet,
			params Task[] dependencies);

		/// <summary>
		///     Run <see cref="OrganizationService.Update" /> asynchronously, and return a reponse object that contains the result.
		///     Accessing the result will block the thread until the process finishes communicating with CRM.
		/// </summary>
		Task<object> UpdateAsync(Entity entity, params Task[] dependencies);

		/// <summary>
		///     Run <see cref="OrganizationService.Delete" /> asynchronously, and return a reponse object that contains the result.
		///     Accessing the result will block the thread until the process finishes communicating with CRM.
		/// </summary>
		Task<object> DeleteAsync(string entityName, Guid id, params Task[] dependencies);

		/// <summary>
		///     Run <see cref="OrganizationService.Associate" /> asynchronously, and return a reponse object that contains the
		///     result.
		///     Accessing the result will block the thread until the process finishes communicating with CRM.
		/// </summary>
		Task<object> AssociateAsync(string entityName, Guid entityId, Relationship relationship,
			EntityReferenceCollection relatedEntities, params Task[] dependencies);

		/// <summary>
		///     Run <see cref="OrganizationService.Disassociate" /> asynchronously, and return a reponse object that contains the
		///     result.
		///     Accessing the result will block the thread until the process finishes communicating with CRM.
		/// </summary>
		Task<object> DisassociateAsync(string entityName, Guid entityId, Relationship relationship,
			EntityReferenceCollection relatedEntities, params Task[] dependencies);

		/// <summary>
		///     Run <see cref="OrganizationService.RetrieveMultiple" /> asynchronously, and return a reponse object that contains
		///     the result.
		///     Accessing the result will block the thread until the process finishes communicating with CRM.
		/// </summary>
		Task<EntityCollection> RetrieveMultipleAsync(QueryBase query, params Task[] dependencies);

		/// <summary>
		///     Run <see cref="OrganizationService.Execute" /> asynchronously,
		///     and return a reponse object that contains the result.
		///     Accessing the result will block the thread until the process finishes communicating with CRM.
		/// </summary>
		/// <typeparam name="TResponseType">The type of the response returned from CRM.</typeparam>
		/// <param name="request">The request to execute.</param>
		/// <param name="dependencies">
		///     [OPTIONAL] A list of dependency operations to check for success status before executing this
		///     one.
		/// </param>
		/// <returns>An enhanced response which can be accessed to retrieve the CRM response.</returns>
		Task<TResponseType> ExecuteAsync<TResponseType>(OrganizationRequest request,
			params Task[] dependencies)
			where TResponseType : OrganizationResponse;

		/// <summary>
		///     Run <see cref="OrganizationService.Execute" /> asynchronously,
		///     and return a reponse object that contains the result.
		///     Accessing the result will block the thread until the process finishes communicating with CRM.
		/// </summary>
		/// <typeparam name="TResponseType">The type of the response returned from CRM.</typeparam>
		/// <param name="request">The request to execute.</param>
		/// <param name="undoFunction">The undo function to be used as an instruction on how to undo the given request.</param>
		/// <param name="dependencies">
		///     [OPTIONAL] A list of dependency operations to check for success status before executing this
		///     one.
		/// </param>
		/// <returns>An enhanced response which can be accessed to retrieve the CRM response.</returns>
		Task<TResponseType> ExecuteAsync<TResponseType>(OrganizationRequest request,
			Func<IOrganizationService, OrganizationRequest, OrganizationRequest> undoFunction,
			params Task[] dependencies)
			where TResponseType : OrganizationResponse;

		/// <summary>
		///     Run <see cref="EnhancedOrgServiceBase.ExecuteBulk" /> asynchronously, and return a reponse object that contains the
		///     result.
		///     Accessing the result will block the thread until the process finishes communicating with CRM.
		/// </summary>
		Task<IDictionary<OrganizationRequest, ExecuteBulkResponse>> ExecuteBulkAsync(
			List<OrganizationRequest> requestsList, bool isReturnResponses, params Task[] dependencies);

		/// <summary>
		///     Run <see cref="EnhancedOrgServiceBase.ExecuteBulk" /> asynchronously, and return a reponse object that contains the
		///     result.
		///     Accessing the result will block the thread until the process finishes communicating with CRM.
		/// </summary>
		Task<IDictionary<OrganizationRequest, ExecuteBulkResponse>> ExecuteBulkAsync(
			List<OrganizationRequest> requestsList, bool isReturnResponses, int bulkSize, params Task[] dependencies);

		/// <summary>
		///     Run <see cref="EnhancedOrgServiceBase.ExecuteBulk" /> asynchronously, and return a reponse object that contains the
		///     result.
		///     Accessing the result will block the thread until the process finishes communicating with CRM.
		/// </summary>
		Task<IDictionary<OrganizationRequest, ExecuteBulkResponse>> ExecuteBulkAsync(
			List<OrganizationRequest> requests,
			bool isReturnResponses = false, int batchSize = 1000, bool isContinueOnError = true,
			Action<int, int, IDictionary<OrganizationRequest, ExecuteBulkResponse>> bulkFinishHandler = null,
			params Task[] dependencies);

		/// <summary>
		///     Run <see cref="EnhancedOrgServiceBase.RetrieveMultiple" />
		///     asynchronously, and return a reponse object that contains the result.
		///     Accessing the result will block the thread until the process finishes communicating with CRM.
		/// </summary>
		Task<IEnumerable<TEntityType>> RetrieveMultipleAsync<TEntityType>(QueryExpression query,
			int limit = -1, params Task[] dependencies)
			where TEntityType : Entity;

		/// <summary>
		///     Run <see cref="EnhancedOrgServiceBase.RetrieveMultipleRangePaged{TEntityType}" />
		///     asynchronously, and return a reponse object that contains the result.
		///     Accessing the result will block the thread until the process finishes communicating with CRM.
		/// </summary>
		Task<IEnumerable<TEntityType>> RetrieveMultipleRangePagedAsync<TEntityType>(QueryExpression query,
			int pageStart = 1, int pageEnd = 1, int pageSize = 5000, params Task[] dependencies)
			where TEntityType : Entity;

		/// <summary>
		///     Run <see cref="EnhancedOrgServiceBase.RetrieveMultiplePage{TEntityType}" />
		///     asynchronously, and return a reponse object that contains the result.
		///     Accessing the result will block the thread until the process finishes communicating with CRM.
		/// </summary>
		Task<IEnumerable<TEntityType>> RetrieveMultiplePageAsync<TEntityType>(QueryExpression query,
			int pageSize = 5000, int page = 1, params Task[] dependencies)
			where TEntityType : Entity;

		/// <summary>
		///     Gets the total number of records for this query.
		/// </summary>
		/// <param name="query">The query.</param>
		/// <returns>The total number of records</returns>
		Task<int> GetRecordsCountAsync(QueryBase query);

		/// <summary>
		///     Gets the total number of pages for this query.
		/// </summary>
		/// <param name="query">The query.</param>
		/// <param name="pageSize">The page size</param>
		/// <returns>The total number of pages</returns>
		Task<int> GetPagesCountAsync(QueryBase query, int pageSize = 5000);

		/// <summary>
		///     Clones the query.
		/// </summary>
		/// <param name="query">The query.</param>
		/// <returns></returns>
		Task<QueryExpression> CloneQueryAsync(QueryBase query);
	}
}
