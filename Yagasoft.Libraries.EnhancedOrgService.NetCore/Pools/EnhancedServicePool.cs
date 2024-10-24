﻿#region Imports

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Xrm.Sdk;
using Yagasoft.Libraries.Common;
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

		public EnhancedServicePool(EnhancedServiceFactory<TService, TEnhancedOrgService> factory, PoolParams poolParams = null)
			: base(factory, poolParams)
		{
			factory.Require(nameof(factory));

			Stats = new OperationStats(this);

			if (poolParams != null)
			{
				ParamHelpers.SetPerformanceParams(poolParams);
			}

			crmPool = new ServicePool<IOrganizationService>(
				new ServiceFactory(factory.Parameters.ConnectionParams, factory.Parameters.PoolParams?.TokenExpiryCheck), poolParams);
		}

		public override void WarmUp()
		{
			crmPool.WarmUp();
			base.WarmUp();
		}

		public override void EndWarmup()
		{
			crmPool.EndWarmup();
			base.EndWarmup();
		}

		public override async Task<TService> GetService()
		{
			return await GetInitialisedService(await base.GetService());
		}

		public override void ReleaseService(IOrganizationService service)
		{
			service.Require(nameof(service));

			if (service is EnhancedOrgServiceBase enhancedOrgServiceBase)
			{
				var releasedService = enhancedOrgServiceBase.ClearConnection();

				if (releasedService != null)
				{
					crmPool.ReleaseService(releasedService);
				}
			}

			if (service is TService thisService)
			{
				base.ReleaseService(thisService);
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
				enhancedOrgServiceBase.ReleaseService = () => ReleaseService(enhancedService);

				try
				{
					enhancedOrgServiceBase.InitialiseConnection(await crmPool.GetService());
				}
				catch
				{
					enhancedOrgServiceBase.ReleaseService();
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

			var enhancedService = enhancedFactory.CreateEnhancedService();
			CreatedServices++;
			return enhancedService;
		}
	}
}
