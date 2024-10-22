#region Imports

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.SerialiseWorkarounds;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Helpers
{
	/// <summary>
	///     Author: Ahmed Elsawalhy (Yagasoft)
	/// </summary>
	internal static class UndoHelper
	{
		internal static readonly Dictionary<Type,
			Func<IOrganizationService, OrganizationRequest, OrganizationRequest>> UndoLogicCache
				= new();

		/// <summary>
		///     Generates an organisation request that reverts the actions taken by the given request.
		/// </summary>
		/// <param name="service">The service.</param>
		/// <param name="request">The request to create a reverse for.</param>
		/// <returns>The reversion request.</returns>
		/// <exception cref="Exception">Undefined undo logic for request type!</exception>
		internal static OrganizationRequest GenerateReverseRequest(IOrganizationService service,
			OrganizationRequest request)
		{
			if (UndoLogicCache.ContainsKey(request.GetType()))
			{
				return UndoLogicCache[request.GetType()](service, request);
			}

			if (request is ExecuteMultipleRequest)
			{
				var reversedRequest =
					new ExecuteMultipleRequest
					{
						Requests = new OrganizationRequestCollection(),
						Settings =
							new ExecuteMultipleSettings
							{
								ContinueOnError = true,
								ReturnResponses = false
							}
					};

				var requests = ((ExecuteMultipleRequest)request).Requests.ToList();

				requests.ForEach(requestQ => reversedRequest.Requests.Add(GenerateReverseRequest(service, requestQ)));

				return reversedRequest;
			}

			if (request is CreateRequest)
			{
				if (((CreateRequest)request).Target.Id == Guid.Empty)
				{
					((CreateRequest)request).Target.Id = Guid.NewGuid();
				}

				return new DeleteRequest
					   {
						   Target = ((CreateRequest)request).Target.ToEntityReference()
					   };
			}

			if (request is DeleteRequest)
			{
				var target = ((DeleteRequest)request).Target;
				var oldTarget = service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
				return new CreateRequest
					   {
						   Target = oldTarget
					   };
			}

			if (request is AssociateRequest)
			{
				var requestTemp = (AssociateRequest)request;
				return new DisassociateRequest
					   {
						   Target = requestTemp.Target,
						   Relationship = requestTemp.Relationship,
						   RelatedEntities = requestTemp.RelatedEntities
					   };
			}

			if (request is DisassociateRequest)
			{
				var requestTemp = (DisassociateRequest)request;
				return new AssociateRequest
					   {
						   Target = requestTemp.Target,
						   Relationship = requestTemp.Relationship,
						   RelatedEntities = requestTemp.RelatedEntities
					   };
			}

			if (request is UpdateRequest)
			{
				var target = ((UpdateRequest)request).Target;
				var columns = target.Attributes
					.Where(pair => pair.Value != null)
					.Select(pair => pair.Key).ToArray();
				var oldTarget = service.Retrieve(target.LogicalName, target.Id, new ColumnSet(columns));
				return new UpdateRequest
					   {
						   Target = oldTarget
					   };
			}

			throw new Exception("Undefined undo for request type!");
		}
	}
}
