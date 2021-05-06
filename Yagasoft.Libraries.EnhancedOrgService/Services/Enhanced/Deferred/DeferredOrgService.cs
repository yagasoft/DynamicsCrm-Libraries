using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Yagasoft.Libraries.Common;
using Yagasoft.Libraries.EnhancedOrgService.Exceptions;
using Yagasoft.Libraries.EnhancedOrgService.Response.Tokens;

namespace Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced.Deferred
{
    public class DeferredOrgService : IDeferredOrgService
    {
	    public IEnumerable<OrganizationRequest> DeferredRequests
	    {
		    get
		    {
			    ValidateDeferredQueueState();
			    return deferredRequests?
				    .Cast<OrganisationRequestToken<OrganizationResponse>>()
				    .Select(e => e.Request);

		    }
	    }

	    private readonly EnhancedOrgServiceBase enhancedOrgServiceBase;
	    private readonly Action cancelAction;
	    private List<IToken<OrganizationResponse>> deferredRequests;

	    protected internal DeferredOrgService(EnhancedOrgServiceBase enhancedOrgServiceBase, Action cancelAction)
	    {
		    this.enhancedOrgServiceBase = enhancedOrgServiceBase;
		    this.cancelAction = cancelAction;
		    deferredRequests = new List<IToken<OrganizationResponse>>();
	    }

		protected virtual void ValidateDeferredQueueState()
		{
			if (deferredRequests == null)
			{
				throw new StateException("Deferred queue is in an invalid state. Restart the queue first.");
			}
		}

		public virtual IDictionary<OrganizationRequest, OrganisationRequestToken<OrganizationResponse>>
			ExecuteDeferredRequests(int bulkSize = 1000)
		{
			ValidateDeferredQueueState();

		    IDictionary<OrganizationRequest, ExecuteBulkResponse> bulkResponse;

		    using (var service = enhancedOrgServiceBase.GetService())
		    {
		        bulkResponse = deferredRequests
		            .Cast<OrganisationRequestToken<OrganizationResponse>>()
		            .Select(e => e.Request)
		            .ExecuteTransaction(service, true, bulkSize);
		    }

			var responses = deferredRequests
				.Cast<OrganisationRequestToken<OrganizationResponse>>()
				.ToDictionary(
					e => e.Request,
					e =>
					{
						e.Value = bulkResponse[e.Request].Response;
						return e;
					});

			// TODO set response token as ready for consumption, else error
			CancelDeferredRequests();

			return responses;
		}

		public virtual void CancelDeferredRequests()
		{
			deferredRequests?.Clear();
			deferredRequests = null;
			cancelAction?.Invoke();
		}

		public virtual OrganisationRequestToken<CreateResponse> CreateDeferred(Entity entity)
		{
			ValidateDeferredQueueState();
			var token =
				new OrganisationRequestToken<CreateResponse>
				{
					Request =
						new CreateRequest
						{
							Target = entity.Copy()
						}
				};
			deferredRequests.Add(token);
			return token;
		}

		public virtual OrganisationRequestToken<UpdateResponse> UpdateDeferred(Entity entity)
		{
			ValidateDeferredQueueState();
			var token =
				new OrganisationRequestToken<UpdateResponse>
				{
					Request =
						new UpdateRequest
						{
							Target = entity.Copy()
						}
				};
			deferredRequests.Add(token);
			return token;
		}

		public virtual OrganisationRequestToken<UpsertResponse> UpsertDeferred(Entity entity)
		{
			ValidateDeferredQueueState();
			var token =
				new OrganisationRequestToken<UpsertResponse>
				{
					Request =
						new UpsertRequest
						{
							Target = entity.Copy()
						}
				};
			deferredRequests.Add(token);
			return token;
		}

		public virtual OrganisationRequestToken<DeleteResponse> DeleteDeferred(string entityName, Guid id)
		{
			ValidateDeferredQueueState();
			var token =
				new OrganisationRequestToken<DeleteResponse>
				{
					Request =
						new DeleteRequest
						{
							Target = new EntityReference(entityName, id)
						}
				};
			deferredRequests.Add(token);
			return token;
		}

		public virtual OrganisationRequestToken<AssociateResponse> AssociateDeferred(string entityName,
			Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
		{
			ValidateDeferredQueueState();
			var token =
				new OrganisationRequestToken<AssociateResponse>
				{
					Request =
						new AssociateRequest
						{
							Target = new EntityReference(entityName, entityId),
							Relationship = relationship.Copy(),
							RelatedEntities = relatedEntities.Copy()
						}
				};
			deferredRequests.Add(token);
			return token;
		}

		public virtual OrganisationRequestToken<DisassociateResponse> DisassociateDeferred(string entityName,
			Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
		{
			ValidateDeferredQueueState();
			var token =
				new OrganisationRequestToken<DisassociateResponse>
				{
					Request =
						new DisassociateRequest
						{
							Target = new EntityReference(entityName, entityId),
							Relationship = relationship.Copy(),
							RelatedEntities = relatedEntities.Copy()
						}
				};
			deferredRequests.Add(token);
			return token;
		}

		public virtual OrganisationRequestToken<TResponse> ExecuteDeferred<TResponse>(OrganizationRequest request)
			where TResponse : OrganizationResponse
		{
			ValidateDeferredQueueState();
			var token = new OrganisationRequestToken<TResponse> { Request = request.Copy() };
			deferredRequests.Add(token);
			return token;
		}
    }
}
