#region Imports

using Yagasoft.Libraries.EnhancedOrgService.Operations;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Pools
{
	/// <summary>
	///     Manages a pool of cached Enhanced Org Services.
	///     Releasing the services to the pool is done manually, or through the 'using' keyword.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	public interface IEnhancedServicePool<out TService> : IServicePool<TService>, IOpStatsAggregate, IOpStatsParent
		where TService : IEnhancedOrgService
	{
		/// <summary>
		///     Clears the cache of the Factory used to initialise the Pool.
		///     If the cache scope is not set to Factory in the Params, an Exception will be thrown.
		/// </summary>
		void ClearFactoryCache();
	}
}
