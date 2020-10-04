#region Imports



#endregion

using System;
using Microsoft.Xrm.Sdk;

namespace Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced.Cache
{
	public interface ICachingOrgService : IEnhancedOrgService
	{
		/// <summary>
		///     Removed an entity from cache.<br />
		/// </summary>
		void RemoveFromCache(Entity record);

		/// <summary>
		///     Removed an entity from cache.<br />
		/// </summary>
		void RemoveFromCache(EntityReference entity);

		/// <summary>
		///     Removed an entity from cache.<br />
		/// </summary>
		void RemoveFromCache(string entityLogicalName, Guid? id);

		/// <summary>
		///     Removed based on request from cache.<br />
		/// </summary>
		void RemoveFromCache(OrganizationRequest request);

		/// <summary>
		///     Removed all entities from cache.<br />
		/// </summary>
		void RemoveAllFromCache();

		/// <summary>
		///     Clears the query's memory cache.<br />
		///     If the cache is not only scoped to this service (factory's 'PrivatePerInstance' setting), an exception is thrown.
		/// </summary>
		void ClearCache();
	}
}
