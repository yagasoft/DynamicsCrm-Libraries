#region Imports

using Microsoft.Xrm.Sdk;
using Yagasoft.Libraries.EnhancedOrgService.Factories;
using Yagasoft.Libraries.EnhancedOrgService.Router;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Pools
{
	public class RoutingPool<TService> : IServicePool<TService>
		where TService : IOrganizationService
	{
		public int CreatedServices => routingService.Nodes.Sum(n => n.Pool.CreatedServices);

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

		private readonly IRoutingService<TService> routingService;

		public RoutingPool(IRoutingService<TService> routingService)
		{
			this.routingService = routingService;
		}

		public void WarmUp()
		{
			routingService.WarmUp();
		}

		public void EndWarmup()
		{
			routingService.EndWarmup();
		}

		public TService GetService()
		{
			return routingService.GetService();
		}

		public void ReleaseService(IOrganizationService service)
		{
			if (service is Services.Enhanced.EnhancedOrgService{IsReleased: false } enhancedService)
			{
				enhancedService.Dispose();
			}
		}

		public void AutoSizeIncrement()
		{
			foreach (var node in routingService.Nodes)
			{
				node.Pool.AutoSizeIncrement();
			}
		}

		public void AutoSizeDecrement()
		{
			foreach (var node in routingService.Nodes)
			{
				node.Pool.AutoSizeDecrement();
			}
		}
	}
}
