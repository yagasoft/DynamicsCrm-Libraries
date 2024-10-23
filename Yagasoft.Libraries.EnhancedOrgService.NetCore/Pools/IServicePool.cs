#region Imports

using Microsoft.Xrm.Sdk;
using Yagasoft.Libraries.EnhancedOrgService.Factories;
using Yagasoft.Libraries.EnhancedOrgService.Pools.WarmUp;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Pools
{
	public interface IServicePool<out TService> : IWarmUp
		where TService : IOrganizationService
	{
		int CreatedServices { get; }
		int CurrentPoolSize { get; }
		bool IsAutoPoolSize { get; set; }
		int MaxPoolSize { get; set; }

		IServiceFactory<TService> Factory { get; }

		/// <summary>
		///     If the pool is empty, creates a new Enhanced Service.<br />
		///     Blocks if the pool exceeds capacity until a service is released.<br />
		///     If a timeout is specified, a new service is forcibly created after the timeout regardless of the limit.
		///     This is ensures protection against deadlocks.
		/// </summary>
		TService GetService();

		/// <summary>
		///     Puts the service back into the pool for re-use.<br />
		///     The state of the service becomes invalid; so it should not be used after calling this method.
		/// </summary>
		/// <param name="service">Service to release.</param>
		void ReleaseService(IOrganizationService service);

		/// <summary>
		///     Increments the <see cref="MaxPoolSize" /> after a successful request.
		///     Does not exceed the max size set in the parameters.
		/// </summary>
		void AutoSizeIncrement();

		/// <summary>
		///     Decrements the <see cref="MaxPoolSize" /> after a failed request.
		/// </summary>
		void AutoSizeDecrement();
	}
}
