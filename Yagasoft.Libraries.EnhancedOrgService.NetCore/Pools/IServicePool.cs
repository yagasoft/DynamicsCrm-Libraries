#region Imports

using Microsoft.Xrm.Sdk;

using Yagasoft.Libraries.EnhancedOrgService.Events;
using Yagasoft.Libraries.EnhancedOrgService.Events.EventArgs;
using Yagasoft.Libraries.EnhancedOrgService.Factories;
using Yagasoft.Libraries.EnhancedOrgService.Params;
using Yagasoft.Libraries.EnhancedOrgService.Services.SelfDisposing;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Pools
{
	public interface IServicePool<TService>
		where TService : IOrganizationService
	{
		int CurrentPoolSize { get; }
		bool IsAutoPoolSize { get; internal set; }
		int MaxPoolSize { get; set; }

		IServiceFactory<TService> Factory { get; }
		int? RecommendedDegreesOfParallelism { get; }
		
		PoolParams PoolParams { get; }
		
		EventHandler<IOrganizationService, OperationStatusEventArgs, IOrganizationService>? OperationsEventHandler { get; protected set; }

		/// <summary>
		///     If the pool is empty, creates a new Enhanced Service.<br />
		///     Blocks if the pool exceeds capacity until a service is released.<br />
		///     If a timeout is specified, a new service is forcibly created after the timeout regardless of the limit.
		///     This is ensures protection against deadlocks.
		/// </summary>
		Task<TService> GetService();

		/// <summary>
		///     Gets a service (<see cref="GetService" />) and wraps it in a 'disposing' logic to return this pool once done.
		///     Must call 'dispose', or embed service in a 'using' block.
		/// </summary>
		Task<IDisposableService> GetSelfDisposingService();

		/// <summary>
		///     Puts the service back into the pool for re-use.<br />
		///     The state of the service becomes invalid; so it should not be used after calling this method.
		/// </summary>
		/// <param name="service">Service to release.</param>
		Task ReleaseService(IOrganizationService service);
	}
}
