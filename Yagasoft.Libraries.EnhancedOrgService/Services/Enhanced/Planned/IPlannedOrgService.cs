using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.Planning;
using Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.SdkMocks;

namespace Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced.Planned
{
	public interface IPlannedOrgService
	{
		/// <summary>
		/// Plans the <see cref="IOrganizationService.Create"/> operation.
		/// </summary>
		PlannedValue PlanCreate(Entity entity);

		/// <summary>
		/// Plans the <see cref="IOrganizationService.Retrieve"/> operation.
		/// </summary>
		PlannedEntity PlanRetrieve(string entityName, Guid id, ColumnSet columnSet);

		/// <summary>
		/// Plans the <see cref="IOrganizationService.Update"/> operation.
		/// </summary>
		void PlanUpdate(Entity entity);

		/// <summary>
		/// Plans the <see cref="IOrganizationService.Delete"/> operation.
		/// </summary>
		void PlanDelete(string entityName, Guid id);

		/// <summary>
		/// Plans the <see cref="IOrganizationService.Associate"/> operation.
		/// </summary>
		void PlanAssociate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities);

		/// <summary>
		/// Plans the <see cref="IOrganizationService.Disassociate"/> operation.
		/// </summary>
		void PlanDisassociate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities);

		/// <summary>
		/// Plans the <see cref="IOrganizationService.RetrieveMultiple"/> operation.
		/// </summary>
		PlannedEntityCollection PlanRetrieveMultiple(QueryBase query);

		/// <summary>
		/// Plans the <see cref="IOrganizationService.Execute"/> operation.
		/// </summary>
		PlannedResponse PlanExecute(OrganizationRequest request);

		/// <summary>
		///     Cancels the plan and clears the queue.
		/// </summary>
		void CancelPlanning();

		/// <summary>
		///     Executes the plan in CRM, and returns the result as operation ID and <see cref="OrganizationResponse" /> map.<br />
		///     Create, Retrieve ... etc. return <see cref="PlannedValue" /> not <see cref="PlannedOperation" /> (unlike Execute);
		///     so access the <see cref="PlannedValue.ParentId" /> property instead to get the operation ID.
		/// </summary>
		IDictionary<Guid, OrganizationResponse> ExecutePlan();
	}
}
