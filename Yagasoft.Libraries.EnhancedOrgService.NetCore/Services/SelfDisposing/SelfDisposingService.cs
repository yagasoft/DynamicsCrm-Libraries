﻿#region Imports

using System;
using Microsoft.PowerPlatform.Dataverse.Client;
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

		public async Task<Guid> CreateAsync(Entity entity)
		{
			return await (service as IOrganizationServiceAsync2)?.CreateAsync(entity);
		}

		public async Task<Entity> RetrieveAsync(string entityName, Guid id, ColumnSet columnSet)
		{
			return await (service as IOrganizationServiceAsync2)?.RetrieveAsync(entityName, id, columnSet);
		}

		public async Task UpdateAsync(Entity entity)
		{
			await (service as IOrganizationServiceAsync2)?.UpdateAsync(entity);
		}

		public async Task DeleteAsync(string entityName, Guid id)
		{
			await (service as IOrganizationServiceAsync2)?.DeleteAsync(entityName, id);
		}

		public async Task<OrganizationResponse> ExecuteAsync(OrganizationRequest request)
		{
			return await (service as IOrganizationServiceAsync2)?.ExecuteAsync(request);
		}

		public async Task AssociateAsync(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
		{
			await (service as IOrganizationServiceAsync2)?.AssociateAsync(entityName, entityId, relationship, relatedEntities);
		}

		public async Task DisassociateAsync(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
		{
			await (service as IOrganizationServiceAsync2)?.DisassociateAsync(entityName, entityId, relationship, relatedEntities);
		}

		public async Task<EntityCollection> RetrieveMultipleAsync(QueryBase query)
		{
			return await (service as IOrganizationServiceAsync2)?.RetrieveMultipleAsync(query);
		}

		public async Task AssociateAsync(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities,
			CancellationToken cancellationToken)
		{
			await (service as IOrganizationServiceAsync2)?.AssociateAsync(entityName, entityId, relationship, relatedEntities, cancellationToken);
		}

		public async Task<Guid> CreateAsync(Entity entity, CancellationToken cancellationToken)
		{
			return await (service as IOrganizationServiceAsync2)?.CreateAsync(entity, cancellationToken);
		}

		public async Task<Entity> CreateAndReturnAsync(Entity entity, CancellationToken cancellationToken)
		{
			return await (service as IOrganizationServiceAsync2)?.CreateAndReturnAsync(entity, cancellationToken);
		}

		public async Task DeleteAsync(string entityName, Guid id, CancellationToken cancellationToken)
		{
			await (service as IOrganizationServiceAsync2)?.DeleteAsync(entityName, id, cancellationToken);
		}

		public async Task DisassociateAsync(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities,
			CancellationToken cancellationToken)
		{
			await (service as IOrganizationServiceAsync2)?.DisassociateAsync(entityName, entityId, relationship, relatedEntities, cancellationToken);
		}

		public async Task<OrganizationResponse> ExecuteAsync(OrganizationRequest request, CancellationToken cancellationToken)
		{
			return await (service as IOrganizationServiceAsync2)?.ExecuteAsync(request, cancellationToken);
		}

		public async Task<Entity> RetrieveAsync(string entityName, Guid id, ColumnSet columnSet, CancellationToken cancellationToken)
		{
			return await (service as IOrganizationServiceAsync2)?.RetrieveAsync(entityName, id, columnSet, cancellationToken);
		}

		public async Task<EntityCollection> RetrieveMultipleAsync(QueryBase query, CancellationToken cancellationToken)
		{
			return await (service as IOrganizationServiceAsync2)?.RetrieveMultipleAsync(query, cancellationToken);
		}

		public async Task UpdateAsync(Entity entity, CancellationToken cancellationToken)
		{
			await (service as IOrganizationServiceAsync2)?.UpdateAsync(entity, cancellationToken);
		}
	}
}
