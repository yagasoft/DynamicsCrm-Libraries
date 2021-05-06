using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Yagasoft.Libraries.Common;
using Yagasoft.Libraries.EnhancedOrgService.Exceptions;
using Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.Planning;
using Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.SdkMocks;
using Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.SerialiseWorkarounds;

namespace Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced.Planned
{
    public class PlannedOrgService : IPlannedOrgService
    {
	    private Queue<PlannedOperation> executionQueue;
	    private readonly EnhancedOrgServiceBase enhancedOrgServiceBase;
	    private readonly Action cancelAction;

	    protected internal PlannedOrgService(EnhancedOrgServiceBase enhancedOrgServiceBase, Action cancelAction)
	    {
		    this.enhancedOrgServiceBase = enhancedOrgServiceBase;
		    this.cancelAction = cancelAction;
		    executionQueue = new Queue<PlannedOperation>();
	    }

		protected virtual void ValidateExecutionPlanState()
		{
			if (executionQueue == null)
			{
				throw new StateException("Execution planning is in an invalid state. Restart the plan first.");
			}
		}

		public virtual void CancelPlanning()
		{
			executionQueue = null;
			cancelAction?.Invoke();
		}

		public virtual PlannedValue PlanCreate(Entity entity)
		{
			var request = new CreateRequest { Target = entity };
			CreateResponse reponse;
			return PlanOperation(request, pr => pr.GetPlannedValue<PlannedValue>(nameof(reponse.id)));
		}

		public virtual PlannedEntity PlanRetrieve(string entityName, Guid id, ColumnSet columnSet)
		{
			var request =
				new RetrieveRequest
				{
					Target = new EntityReference(entityName, id),
					ColumnSet = columnSet
				};
			RetrieveResponse reponse;
			return PlanOperation(request, pr => pr.GetPlannedValue<PlannedEntity>(nameof(reponse.Entity)));
		}

		public virtual void PlanUpdate(Entity entity)
		{
			var request = new UpdateRequest { Target = entity };
			PlanOperation(request);
		}

		public virtual void PlanDelete(string entityName, Guid id)
		{
			var request = new DeleteRequest { Target = new EntityReference(entityName, id) };
			PlanOperation(request);
		}

		public virtual void PlanAssociate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
		{
			var request =
				new AssociateRequest
				{
					Target = new EntityReference(entityName, entityId),
					Relationship = relationship,
					RelatedEntities = relatedEntities
				};
			PlanOperation(request);
		}

		public virtual void PlanDisassociate(string entityName, Guid entityId, Relationship relationship,
			EntityReferenceCollection relatedEntities)
		{
			var request =
				new DisassociateRequest
				{
					Target = new EntityReference(entityName, entityId),
					Relationship = relationship,
					RelatedEntities = relatedEntities
				};
			PlanOperation(request);
		}

		public virtual PlannedEntityCollection PlanRetrieveMultiple(QueryBase query)
		{
			var request = new RetrieveMultipleRequest { Query = query };
			RetrieveMultipleResponse reponse;
			return PlanOperation(request, pr => pr.GetPlannedValue<PlannedEntityCollection>(nameof(reponse.EntityCollection)));
		}

		public virtual PlannedResponse PlanExecute(OrganizationRequest request)
		{
			return PlanOperation(request);
		}

		protected virtual PlannedResponse PlanOperation(OrganizationRequest plannedOperation)
		{
			return PlanOperation<PlannedResponse>(plannedOperation);
		}

		protected virtual T PlanOperation<T>(OrganizationRequest plannedOperation, Func<PlannedResponse, T> getPlannedValue = null)
			where T : IPlannedValue
		{
			ValidateExecutionPlanState();

			var plannedResponse = new PlannedResponse();
			var plannedValue = getPlannedValue == null ? (IPlannedValue)plannedResponse : getPlannedValue(plannedResponse);

			executionQueue.Enqueue(
				new PlannedOperation
				{
					Request = plannedOperation.Copy().Mock<MockOrgRequest>(),
					Response = plannedResponse
				});

			return (T)plannedValue;
		}

		public virtual IDictionary<Guid, OrganizationResponse> ExecutePlan()
		{
			ValidateExecutionPlanState();

			var serialised = executionQueue.ToList().SerialiseContractJson(true,
				surrogate: new DateTimeCrmContractSurrogateCustom());

			if (serialised.IsEmpty())
			{
				throw new InvalidPluginExecutionException("Could not deserialise planned execution operations."
					+ " Possibly because one of the serialised types could not be found through the sandbox plugin.");
			}

			var request =
				new OrganizationRequest("ys_LibrariesExecutePlannedOperations")
				{
					Parameters = new ParameterCollection { { "ExecutionPlan", serialised.Compress() } }
				};

		    string response;

		    using (var service = enhancedOrgServiceBase.GetService())
		    {
		        response = ((string)service.Execute(request)["SerialisedResult"]).Decompress();
		    }

			var result = response.DeserialiseContractJson<MockDictionary>(true,
				surrogate: new DateTimeCrmContractSurrogateCustom())
				.ToDictionary(e => Guid.Parse(e.Key), e => e.Value.Unmock<OrganizationResponse>());

			CancelPlanning();

			return result;
		}
    }
}
