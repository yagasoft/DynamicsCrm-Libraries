#region Imports

using System;
using System.Linq;
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
	}
}
