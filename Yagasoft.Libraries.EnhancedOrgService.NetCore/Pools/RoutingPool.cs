#region Imports

using Microsoft.Xrm.Sdk;
using Yagasoft.Libraries.EnhancedOrgService.Events;
using Yagasoft.Libraries.EnhancedOrgService.Events.EventArgs;
using Yagasoft.Libraries.EnhancedOrgService.Exceptions;
using Yagasoft.Libraries.EnhancedOrgService.Factories;
using Yagasoft.Libraries.EnhancedOrgService.Params;
using Yagasoft.Libraries.EnhancedOrgService.Router;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced;
using Yagasoft.Libraries.EnhancedOrgService.Services.SelfDisposing;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Pools
{
	public class RoutingPool<TService> : IServicePool<TService>
		where TService : IOrganizationService
	{
		public int CurrentPoolSize => routingService.Nodes.Sum(n => n.Pool.CurrentPoolSize);

		public virtual bool IsAutoPoolSize
		{
			get => routingService.Nodes.Any(n => n.Pool.IsAutoPoolSize);
			set
			{
				foreach (var node in routingService.Nodes)
				{
					node.Pool.IsAutoPoolSize = value;
				}
			}
		}

		public virtual int MaxPoolSize
		{
			get => routingService.Nodes.Sum(n => n.Pool.MaxPoolSize);
			set
			{
				foreach (var node in routingService.Nodes)
				{
					node.Pool.MaxPoolSize = value;
				}
			}
		}

		public IServiceFactory<TService> Factory =>
			throw new NotSupportedException("This pool does not require a factory to run.");

		public int? RecommendedDegreesOfParallelism
		{
			get => routingService.Nodes.Sum(n => n.Pool.RecommendedDegreesOfParallelism);
		}

		public PoolParams PoolParams { get; }

		public EventHandler<IOrganizationService, OperationStatusEventArgs, IOrganizationService>? OperationsEventHandler { get; set; }

		private readonly IRoutingService<TService> routingService;

		public RoutingPool(IRoutingService<TService> routingService)
		{
			this.routingService = routingService;
			OperationsEventHandler += OnOperationStatusChanged;
		}

		public async Task<TService> GetService()
		{
			return await routingService.GetService();
		}

		public async Task<IDisposableService> GetSelfDisposingService()
		{
			var service = await GetService();
			
			if (service == null)
			{
				throw new StateException("Failed to find an internal CRM service.");
			}

			return new SelfDisposingService(service, () => ReleaseService(service));
		}

		public void ReleaseService(IOrganizationService service)
		{
			if (service is SelfDisposingService disposableService)
			{
				disposableService.Dispose();
			}
		}
		
		protected virtual void OnOperationStatusChanged(IOrganizationService sender, OperationStatusEventArgs e, IOrganizationService s)
		{
			IServicePool<IOrganizationService>? pool;
			var service = s as SelfDisposingService;
				
			while (true)
			{
				if (service?.Service is SelfDisposingService disposingService)
				{
					service = disposingService;
					continue;
				}

				var enhancedService = s as EnhancedOrgServiceBase;
				
				if (enhancedService?.CrmService is SelfDisposingService crmService)
				{
					service = crmService;
					continue;
				}

				pool = service?.ServicePool;
				
				break;
			}

			pool?.OperationsEventHandler?.Invoke(sender, e, s);
		}
	}
}
