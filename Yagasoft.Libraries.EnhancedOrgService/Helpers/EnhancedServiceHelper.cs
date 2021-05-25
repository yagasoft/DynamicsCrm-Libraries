#region Imports

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Yagasoft.Libraries.Common;
using Yagasoft.Libraries.EnhancedOrgService.Factories;
using Yagasoft.Libraries.EnhancedOrgService.Params;
using Yagasoft.Libraries.EnhancedOrgService.Pools;
using Yagasoft.Libraries.EnhancedOrgService.Router;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced.Cache;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Helpers
{
	/// <summary>
	///     Provides methods to quickly and easily create Enhanced Organisation Services.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	public static class EnhancedServiceHelper
	{
		/// <summary>
		///     <inheritdoc cref="ServiceParams.AutoSetMaxPerformanceParams" /><br />
		///     <b>This setting is applied if 'true', and only when creating the upcoming connections.</b>
		/// </summary>
		public static bool AutoSetMaxPerformanceParams { get; set; }

		public static IEnhancedServicePool<IEnhancedOrgService> GetPool(string connectionString, int poolSize = 2)
		{
			connectionString.RequireFilled(nameof(connectionString));
			return GetPool(BuildBaseParams(connectionString, poolSize));
		}

		public static IEnhancedServicePool<IEnhancedOrgService> GetPool(string connectionString, PoolParams poolParams)
		{
			connectionString.RequireFilled(nameof(connectionString));
			poolParams.Require(nameof(poolParams));
			return GetPool(BuildBaseParams(connectionString, null, poolParams));
		}

		public static IEnhancedServicePool<IEnhancedOrgService> GetPool(ConnectionParams connectionParams, PoolParams poolParams)
		{
			connectionParams.Require(nameof(connectionParams));
			poolParams.Require(nameof(poolParams));
			return GetPool(BuildBaseParams(null, null, poolParams, connectionParams));
		}

		public static IEnhancedServicePool<IEnhancedOrgService> GetPool(ServiceParams serviceParams)
		{
			serviceParams.Require(nameof(serviceParams));

			if (AutoSetMaxPerformanceParams)
			{
				serviceParams.AutoSetMaxPerformanceParams();
			}

			var factory = new EnhancedServiceFactory<IEnhancedOrgService, Services.Enhanced.EnhancedOrgService>(serviceParams);
			return new EnhancedServicePool<IEnhancedOrgService, Services.Enhanced.EnhancedOrgService>(factory, serviceParams.PoolParams);
		}

		public static IEnhancedServicePool<ICachingOrgService> GetCachingPool(string connectionString, int poolSize = 2,
			CachingParams cachingParams = null)
		{
			connectionString.RequireFilled(nameof(connectionString));
			return GetCachingPool(BuildBaseParams(connectionString, poolSize, null,
				cachingParams: cachingParams ?? new CachingParams()));
		}

		public static IEnhancedServicePool<ICachingOrgService> GetCachingPool(string connectionString, PoolParams poolParams,
			CachingParams cachingParams = null)
		{
			connectionString.RequireFilled(nameof(connectionString));
			poolParams.Require(nameof(poolParams));
			return GetCachingPool(BuildBaseParams(connectionString, null, poolParams,
				cachingParams: cachingParams ?? new CachingParams()));
		}

		public static IEnhancedServicePool<ICachingOrgService> GetCachingPool(ConnectionParams connectionParams, PoolParams poolParams,
			CachingParams cachingParams = null)
		{
			connectionParams.Require(nameof(connectionParams));
			poolParams.Require(nameof(poolParams));
			return GetCachingPool(BuildBaseParams(null, null, poolParams, connectionParams,
				cachingParams ?? new CachingParams()));
		}

		public static IEnhancedServicePool<ICachingOrgService> GetCachingPool(ServiceParams serviceParams)
		{
			serviceParams.Require(nameof(serviceParams));

			if (AutoSetMaxPerformanceParams)
			{
				serviceParams.AutoSetMaxPerformanceParams();
			}

			var factory = new EnhancedServiceFactory<ICachingOrgService, CachingOrgService>(serviceParams);
			return new EnhancedServicePool<ICachingOrgService, CachingOrgService>(factory, serviceParams.PoolParams);
		}

		public static IEnhancedOrgService GetPoolingService(string connectionString, int poolSize = 2)
		{
			connectionString.RequireFilled(nameof(connectionString));
			return GetPoolingService(BuildBaseParams(connectionString, poolSize));
		}

		public static IEnhancedOrgService GetPoolingService(ServiceParams serviceParams)
		{
			serviceParams.Require(nameof(serviceParams));

			if (AutoSetMaxPerformanceParams)
			{
				serviceParams.AutoSetMaxPerformanceParams();
			}

			var pool = new DefaultServicePool(serviceParams);
			var factory = new DefaultEnhancedFactory(serviceParams);
			return factory.CreateService(pool);
		}

		public static ICachingOrgService GetPoolingCachingService(string connectionString, int poolSize = 2,
			CachingParams cachingParams = null)
		{
			connectionString.RequireFilled(nameof(connectionString));
			return GetCachingPoolingService(BuildBaseParams(connectionString, poolSize, null,
				cachingParams: cachingParams ?? new CachingParams()));
		}

		public static ICachingOrgService GetCachingPoolingService(ServiceParams serviceParams)
		{
			serviceParams.Require(nameof(serviceParams));

			if (AutoSetMaxPerformanceParams)
			{
				serviceParams.AutoSetMaxPerformanceParams();
			}

			var pool = new DefaultServicePool(serviceParams);
			var factory = new EnhancedServiceFactory<ICachingOrgService, CachingOrgService>(serviceParams);
			return factory.CreateService(pool);
		}

		public static async Task<IEnhancedOrgService> GetSelfBalancingService(string connectionString,
			IEnumerable<IServicePool<IOrganizationService>> pools, RouterRules rules = null)
		{
			connectionString.RequireFilled(nameof(connectionString));
			return await GetSelfBalancingService(BuildBaseParams(connectionString), pools, rules);
		}

		public static async Task<IEnhancedOrgService> GetSelfBalancingService(ServiceParams parameters,
			IEnumerable<IServicePool<IOrganizationService>> pools, RouterRules rules = null)
		{
			parameters.Require(nameof(parameters));
			pools.Require(nameof(pools));

			if (AutoSetMaxPerformanceParams)
			{
				parameters.AutoSetMaxPerformanceParams();
			}

			var routingService = new RoutingService<IOrganizationService>();

			foreach (var pool in pools)
			{
				routingService.AddNode(pool);
			}

			if (rules != null)
			{
				routingService.DefineRules(rules);
			}
			
			await routingService.StartRouter();

			var routingPool = new RoutingPool<IOrganizationService>(routingService);

			return new EnhancedServiceFactory<IEnhancedOrgService, Services.Enhanced.EnhancedOrgService>(parameters)
				.CreateService(routingPool);
		}

		private static ServiceParams BuildBaseParams(string connectionString, int? poolSize = null,
			PoolParams poolParams = null, ConnectionParams connectionParams = null,
			CachingParams cachingParams = null)
		{
			var parameters =
				new ServiceParams
				{
					ConnectionParams = connectionParams ?? new ConnectionParams { ConnectionString = connectionString },
					PoolParams = poolParams ?? (poolSize == null ? null : new PoolParams { PoolSize = poolSize })
				};

			if (cachingParams != null)
			{
				parameters.IsCachingEnabled = true;
				parameters.CachingParams = cachingParams;
			}

			if (AutoSetMaxPerformanceParams)
			{
				parameters.AutoSetMaxPerformanceParams();
			}

			return parameters;
		}
	}
}
