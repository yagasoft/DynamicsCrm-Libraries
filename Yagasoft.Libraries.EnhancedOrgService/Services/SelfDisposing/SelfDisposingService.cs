#region Imports

using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Yagasoft.Libraries.Common;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Services.SelfDisposing
{
	public class SelfDisposingService : IDisposableService
	{
		private readonly IOrganizationService service;
		private readonly Action disposer;

		internal SelfDisposingService(IOrganizationService service, Action disposer)
		{
			service.Require(nameof(service));
			disposer.Require(nameof(disposer));

			this.service = service;
			this.disposer = disposer;
		}

		public Guid Create(Entity entity)
		{
			return service.Create(entity);
		}

		public Entity Retrieve(string entityName, Guid id, ColumnSet columnSet)
		{
			return service.Retrieve(entityName, id, columnSet);
		}

		public void Update(Entity entity)
		{
			service.Update(entity);
		}

		public void Delete(string entityName, Guid id)
		{
			service.Delete(entityName, id);
		}

		public OrganizationResponse Execute(OrganizationRequest request)
		{
			return service.Execute(request);
		}

		public void Associate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
		{
			service.Associate(entityName, entityId, relationship, relatedEntities);
		}

		public void Disassociate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
		{
			service.Disassociate(entityName, entityId, relationship, relatedEntities);
		}

		public EntityCollection RetrieveMultiple(QueryBase query)
		{
			return service.RetrieveMultiple(query);
		}

		public void Dispose()
		{
			disposer.Invoke();
		}
	}
}
