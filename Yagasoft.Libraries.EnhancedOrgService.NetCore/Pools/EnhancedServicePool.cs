#region Imports

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Xrm.Sdk;
using Yagasoft.Libraries.Common;
using Yagasoft.Libraries.EnhancedOrgService.Events;
using Yagasoft.Libraries.EnhancedOrgService.Events.EventArgs;
using Yagasoft.Libraries.EnhancedOrgService.Exceptions;
using Yagasoft.Libraries.EnhancedOrgService.Factories;
using Yagasoft.Libraries.EnhancedOrgService.Helpers;
using Yagasoft.Libraries.EnhancedOrgService.Operations;
using Yagasoft.Libraries.EnhancedOrgService.Params;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Pools
{
	/// <inheritdoc cref="IEnhancedServicePool{TService}" />
	public class EnhancedServicePool<TService, TEnhancedOrgService> : ServicePool<TService>, IEnhancedServicePool<TService>
		where TService : IEnhancedOrgService
		where TEnhancedOrgService : EnhancedOrgServiceBase, TService
	{
		public IOperationStats Stats { get; }

		public virtual IEnumerable<IOperationStats> StatTargets => statServices;

		private readonly ServicePool<IOrganizationService> crmPool;

		private readonly HashSet<IOperationStats> statServices = new();

		public EnhancedServicePool(EnhancedServiceFactory<TService, TEnhancedOrgService> factory, int poolSize = -1)
			: this(factory, new PoolParams { PoolSize = poolSize })
		{ }

		public EnhancedServicePool(EnhancedServiceFactory<TService, TEnhancedOrgService> factory, PoolParams poolParams)
			: base(factory, poolParams)
		{
			factory.Require(nameof(factory));

			Stats = new OperationStats(this);

			ParamHelpers.SetPerformanceParams(poolParams);

			crmPool = new ServicePool<IOrganizationService>(new ServiceFactory(factory.ServiceParams), poolParams);
		}

		public override async Task<TService> GetService()
		{
			return await GetInitialisedService(await base.GetService());
		}

		public override async Task ReleaseService(IOrganizationService service)
		{
			service.Require(nameof(service));

			if (service is EnhancedOrgServiceBase enhancedOrgServiceBase)
			{
				var releasedService = enhancedOrgServiceBase.ClearConnection();

				if (releasedService != null)
				{
					await crmPool.ReleaseService(releasedService);
				}
			}

			if (service is TService thisService)
			{
				await base.ReleaseService(thisService);
			}
		}

		public void ClearFactoryCache()
		{
			(Factory as IEnhancedServiceFactory<TService>)?.ClearCache();
		}

		private async Task<TService> GetInitialisedService(TService enhancedService = default)
		{
			if (enhancedService == null)
			{
				enhancedService = GetEnhancedService();
			}

			if (enhancedService is EnhancedOrgServiceBase enhancedOrgServiceBase)
			{
				async Task Action() => await ReleaseService(enhancedService);

				enhancedOrgServiceBase.ReleaseService = Action;

				try
				{
					enhancedOrgServiceBase.InitialiseConnection(await crmPool.GetService());
				}
				catch
				{
					await enhancedOrgServiceBase.ReleaseService();
					throw;
				}
			}

			if (enhancedService is EnhancedOrgServiceBase eventService)
			{
				statServices.Add(eventService);
				(Stats as OperationStats)?.Propagate();
			}

			return enhancedService;
		}

		private TService GetEnhancedService()
		{
			if (Factory is not EnhancedServiceFactory<TService, TEnhancedOrgService> enhancedFactory)
			{
				throw new StateException("Unable to create a service due to type mismatch on factory result.");
			}

			return enhancedFactory.CreateEnhancedService();
		}
	}
}
