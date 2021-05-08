#region Imports

using Microsoft.Xrm.Sdk;
using Yagasoft.Libraries.EnhancedOrgService.Operations;
using Yagasoft.Libraries.EnhancedOrgService.Pools;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Factories
{
	/// <summary>
	///     Handles creating Enhanced Org Services and their initialisation.
	///     No caching of connections is used; use <see cref="IEnhancedServicePool{TService}" /> instead.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	public interface IEnhancedServiceFactory<out TService> : IServiceFactory<TService>, IOpStatsAggregate, IOpStatsParent
		where TService : IEnhancedOrgService
	{
		/// <summary>
		///     Creates a Service using the factory parameters.<br />
		///     Provide a connection pool to use for internally managed pooling within the created service;
		///     otherwise, a single connection is used internally.<br />
		///     Alternativelty, Refer to <see cref="IServicePool{TService}.GetService" /> and
		///     <see cref="IServicePool{TService}.ReleaseService" /> for explicit pool control.
		/// </summary>
		TService CreateService(IServicePool<IOrganizationService> servicePool);

		/// <summary>
		///     Clears the memory cache on the level of the factory and any services created.
		/// </summary>
		void ClearCache();
	}
}
