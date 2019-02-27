using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yagasoft.Libraries.Common;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;

namespace Yagasoft.Libraries.EnhancedOrgService.Services
{
	internal class SelfEnqueuingService : IOrganizationService, IDisposable
    {
	    private readonly BlockingQueue<IOrganizationService> queue;
	    private readonly IOrganizationService service;

	    internal SelfEnqueuingService(BlockingQueue<IOrganizationService> queue, IOrganizationService service)
	    {
		    this.queue = queue;
		    this.service = service;
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
		    queue.Enqueue(service);
	    }
    }
}
