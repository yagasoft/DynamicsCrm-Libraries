#region Imports

using Yagasoft.Libraries.EnhancedOrgService.Factories;
using Yagasoft.Libraries.EnhancedOrgService.Operations;
using Yagasoft.Libraries.EnhancedOrgService.Pools.WarmUp;
using Yagasoft.Libraries.EnhancedOrgService.Response.Operations;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced.Transactions;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Pools
{
	/// <summary>
	///     Manages a pool of cached Enhanced Org Services.
	///     Releasing the services to the pool is done manually, or through the 'using' keyword.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	public interface IEnhancedServicePool<out TService> : IOpStatsAggregate, IOpStatsParent, IWarmUp
		where TService : IEnhancedOrgService
	{
		int CreatedServices { get; }
		int CurrentPoolSize { get; }
		IEnhancedServiceFactory<TService> Factory { get; }

		/// <summary>
		///     If the pool is empty, creates a new Enhanced Service.
		///     Blocks if the pool exceeds capacity until a service is released.<br />
		///     The threads are the actual CRM services used to communicate with CRM internally.
		///     This is useful for parallel requests when threading is used.
		///     Blocks if the threads are busy until a request has finished.
		/// </summary>
		/// <param name="threads">Number of internal CRM services to create.</param>
		TService GetService(int threads = 1);

		/// <summary>
		///     Clears the cache of the Factory used to initialise the Pool.
		///     If the cache scope is not set to Factory in the Params, an Exception will be thrown.
		/// </summary>
		void ClearFactoryCache();

		/// <summary>
		///     Puts the service back into the pool for re-use.
		///     The state of the service becomes invalid; so it should not be used after calling this method.
		/// </summary>
		/// <param name="service">Service to release.</param>
		void ReleaseService(IEnhancedOrgService service);
	}
}
