#region Imports

using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Yagasoft.Libraries.EnhancedOrgService.Response.Tokens;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced.Deferred
{
	public interface IDeferredOrgService
	{
		IEnumerable<OrganizationRequest> GetDeferredRequests();

		/// <summary>
		///     Executes all deferred requests in a transaction.
		/// </summary>
		/// <param name="bulkSize">[Optional] The number of requests to execute at once every internal iteration.</param>
		/// <returns>A map of the deferred organisation requests and their response tokens.</returns>
		IDictionary<OrganizationRequest, OrganisationRequestToken<OrganizationResponse>> ExecuteDeferredRequests(int bulkSize = 1000);

		/// <summary>
		///     Cancels all deferred requests and clears the queue.
		/// </summary>
		void CancelDeferredRequests();

		/// <summary>
		///     Defers the create for later execution.
		/// </summary>
		/// <returns>A token of the record's ID whose value can be accessed after the deferred execution has finished.</returns>
		OrganisationRequestToken<CreateResponse> CreateDeferred(Entity entity);

		/// <summary>
		///     Defers the update for later execution.
		/// </summary>
		OrganisationRequestToken<UpdateResponse> UpdateDeferred(Entity entity);

		/// <summary>
		///     Defers the upsert for later execution.
		/// </summary>
		OrganisationRequestToken<UpsertResponse> UpsertDeferred(Entity entity);

		/// <summary>
		///     Defers the delete for later execution.
		/// </summary>
		OrganisationRequestToken<DeleteResponse> DeleteDeferred(string entityName, Guid id);

		/// <summary>
		///     Defers the association for later execution.
		/// </summary>
		OrganisationRequestToken<AssociateResponse> AssociateDeferred(string entityName,
			Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities);

		/// <summary>
		///     Defers the disassociation for later execution.
		/// </summary>
		OrganisationRequestToken<DisassociateResponse> DisassociateDeferred(string entityName,
			Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities);

		/// <summary>
		///     Defers the Organisation Request for later execution.
		/// </summary>
		/// <returns>A token of the organisation response whose value can be accessed after the deferred execution has finished.</returns>
		OrganisationRequestToken<TResponse> ExecuteDeferred<TResponse>(OrganizationRequest request)
			where TResponse : OrganizationResponse;
	}
}
