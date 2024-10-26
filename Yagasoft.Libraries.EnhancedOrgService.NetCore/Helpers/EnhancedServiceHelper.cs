#region Imports

using System;
using System.Collections.Generic;
using System.Linq;
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

		public static IEnhancedServicePool<IEnhancedOrgService> GetPool(string connectionString, int poolSize = -1)
		{
			return GetPool(connectionString, poolSize < 1, poolSize);
		}

		public static IEnhancedServicePool<IEnhancedOrgService> GetPool(string connectionString, bool isAutoSize = true, int maxPoolSize = -1)
		{
			connectionString.RequireFilled(nameof(connectionString));
			return GetEnhancedPool(BuildBaseParams(connectionString, isAutoSize, maxPoolSize));
		}

		public static IEnhancedServicePool<IEnhancedOrgService> GetPool(string connectionString, PoolParams poolParams)
		{
			connectionString.RequireFilled(nameof(connectionString));
			poolParams.Require(nameof(poolParams));
			return GetEnhancedPool(BuildBaseParams(connectionString, false, null, poolParams));
		}

		public static IEnhancedServicePool<IEnhancedOrgService> GetPool(ConnectionParams connectionParams, PoolParams poolParams)
		{
			connectionParams.Require(nameof(connectionParams));
			poolParams.Require(nameof(poolParams));
			return GetEnhancedPool(BuildBaseParams(null, false, null, poolParams, connectionParams));
		}

		public static IServicePool<IOrganizationService> GetPool(SelfBalancingParams serviceParams)
		{
			serviceParams.Require(nameof(serviceParams));

			if (AutoSetMaxPerformanceParams)
			{
				serviceParams.AutoSetMaxPerformanceParams();
			}

			var factory =
				new ServiceFactory(
					new ServiceParams
					{
						ConnectionParams = serviceParams.ConnectionParams,
						PoolParams = serviceParams.PoolParams
					});
			return new ServicePool<IOrganizationService>(factory, serviceParams.PoolParams);
		}

		public static IEnhancedServicePool<IEnhancedOrgService> GetEnhancedPool(ServiceParams serviceParams)
		{
			serviceParams.Require(nameof(serviceParams));
			serviceParams.PoolParams.Require(nameof(serviceParams.PoolParams));

			if (AutoSetMaxPerformanceParams)
			{
				serviceParams.AutoSetMaxPerformanceParams();
			}

			var factory = new EnhancedServiceFactory<IEnhancedOrgService, Services.Enhanced.EnhancedOrgService>(serviceParams);
			return new EnhancedServicePool<IEnhancedOrgService, Services.Enhanced.EnhancedOrgService>(factory, serviceParams.PoolParams!);
		}

		public static IEnhancedServicePool<ICachingOrgService> GetCachingPool(string connectionString, int poolSize = -1,
			CachingParams cachingParams = null)
		{
			return GetCachingPool(connectionString, poolSize < 1, poolSize, cachingParams);
		}

		public static IEnhancedServicePool<ICachingOrgService> GetCachingPool(string connectionString, bool isAutoSize = true, int maxPoolSize = -1,
			CachingParams cachingParams = null)
		{
			connectionString.RequireFilled(nameof(connectionString));
			return GetCachingPool(BuildBaseParams(connectionString, isAutoSize, maxPoolSize, null,
				cachingParams: cachingParams ?? new CachingParams()));
		}

		public static IEnhancedServicePool<ICachingOrgService> GetCachingPool(string connectionString, PoolParams poolParams,
			CachingParams cachingParams = null)
		{
			connectionString.RequireFilled(nameof(connectionString));
			poolParams.Require(nameof(poolParams));
			return GetCachingPool(BuildBaseParams(connectionString, false, null, poolParams,
				cachingParams: cachingParams ?? new CachingParams()));
		}

		public static IEnhancedServicePool<ICachingOrgService> GetCachingPool(ConnectionParams connectionParams, PoolParams poolParams,
			CachingParams cachingParams = null)
		{
			connectionParams.Require(nameof(connectionParams));
			poolParams.Require(nameof(poolParams));
			return GetCachingPool(BuildBaseParams(null, false, null, poolParams, connectionParams,
				cachingParams ?? new CachingParams()));
		}

		public static IEnhancedServicePool<ICachingOrgService> GetCachingPool(ServiceParams serviceParams)
		{
			serviceParams.Require(nameof(serviceParams));
			serviceParams.PoolParams.Require(nameof(serviceParams.PoolParams));

			if (AutoSetMaxPerformanceParams)
			{
				serviceParams.AutoSetMaxPerformanceParams();
			}

			var factory = new EnhancedServiceFactory<ICachingOrgService, CachingOrgService>(serviceParams);
			return new EnhancedServicePool<ICachingOrgService, CachingOrgService>(factory, serviceParams.PoolParams!);
		}

		public static IEnhancedOrgService GetPoolingService(string connectionString, int poolSize)
		{
			return GetPoolingService(connectionString, poolSize < 1, poolSize);
		}

		public static IEnhancedOrgService GetPoolingService(string connectionString, bool isAutoSize = true, int? maxPoolSize = -1)
		{
			connectionString.RequireFilled(nameof(connectionString));
			return GetPoolingService(BuildBaseParams(connectionString, isAutoSize, maxPoolSize));
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
			var service = factory.CreateService(pool);
			service.OperationStatusChanged += pool.OperationsEventHandler;
			
			return service;
		}

		public static ICachingOrgService GetPoolingCachingService(string connectionString, int poolSize = -1,
			CachingParams cachingParams = null)
		{
			return GetPoolingCachingService(connectionString, poolSize < 1, poolSize, cachingParams);
		}

		public static ICachingOrgService GetPoolingCachingService(string connectionString, bool isAutoSize = true, int maxPoolSize = -1,
			CachingParams cachingParams = null)
		{
			connectionString.RequireFilled(nameof(connectionString));
			return GetCachingPoolingService(BuildBaseParams(connectionString, isAutoSize, maxPoolSize, null,
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
			var service = factory.CreateService(pool);
			service.OperationStatusChanged += pool.OperationsEventHandler;
			
			return service;
		}

		public static async Task<IEnhancedOrgService> GetSelfBalancingService(ServiceParams serviceParameters,
			SelfBalancingParams poolParameters, int repeatPools, RouterRules? rules = null)
		{
			return await GetSelfBalancingService(serviceParameters,
				rules,
				Enumerable.Range(0, repeatPools).Select(_ => poolParameters.Copy()).ToArray());
		}

		public static async Task<IEnhancedOrgService> GetSelfBalancingService(ServiceParams serviceParameters,
			params (SelfBalancingParams PoolParameters, int RepeatPools)[] poolParametersRepeated)
		{
			return await GetSelfBalancingService(serviceParameters, null, poolParametersRepeated);
		}

		public static async Task<IEnhancedOrgService> GetSelfBalancingService(ServiceParams serviceParameters,
			RouterRules? rules, params (SelfBalancingParams PoolParameters, int RepeatPools)[] poolParametersRepeated)
		{
			return await GetSelfBalancingService(serviceParameters,
				rules,
				poolParametersRepeated
					.SelectMany(t => Enumerable.Range(0, t.RepeatPools)
						.Select(_ => t.PoolParameters.Copy()))
					.ToArray());
		}

		public static async Task<IEnhancedOrgService> GetSelfBalancingService(ServiceParams serviceParameters,
			params SelfBalancingParams[] poolParameters)
		{
			return await GetSelfBalancingService(serviceParameters, null, poolParameters);
		}

		public static async Task<IEnhancedOrgService> GetSelfBalancingService(ServiceParams serviceParameters,
			RouterRules? rules, params SelfBalancingParams[] poolParameters)
		{
			return await GetSelfBalancingService(serviceParameters, rules, poolParameters.Select(GetPool).ToArray());
		}

		public static async Task<IEnhancedOrgService> GetSelfBalancingService(params SelfBalancingParams[] poolParameters)
		{
			return await GetSelfBalancingService((RouterRules?)null, poolParameters);
		}

		public static async Task<IEnhancedOrgService> GetSelfBalancingService(RouterRules? rules,
			params SelfBalancingParams[] poolParameters)
		{
			return await GetSelfBalancingService(rules, poolParameters.Select(GetPool).ToArray());
		}

		public static async Task<IEnhancedOrgService> GetSelfBalancingService(params IServicePool<IOrganizationService>[] pools)
		{
			return await GetSelfBalancingService((RouterRules?)null, pools);
		}

		public static async Task<IEnhancedOrgService> GetSelfBalancingService(
			RouterRules? rules, params IServicePool<IOrganizationService>[] pools)
		{
			return await GetSelfBalancingService(BuildBaseParams(Guid.NewGuid().ToString()), rules, pools);
		}

		public static async Task<IEnhancedOrgService> GetSelfBalancingService(ServiceParams? serviceParameters,
			params IServicePool<IOrganizationService>[] pools)
		{
			return await GetSelfBalancingService(serviceParameters, null, pools);
		}

		public static async Task<IEnhancedOrgService> GetSelfBalancingService(ServiceParams? serviceParameters,
			RouterRules? rules, params IServicePool<IOrganizationService>[] pools)
		{
			pools.Require(nameof(pools));
			pools.Require(nameof(pools));

			serviceParameters ??= new ServiceParams();
			
			if (AutoSetMaxPerformanceParams)
			{
				serviceParameters.AutoSetMaxPerformanceParams();
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
			var service = new EnhancedServiceFactory<IEnhancedOrgService, Services.Enhanced.EnhancedOrgService>(serviceParameters)
				.CreateService(routingPool);
			service.OperationStatusChanged += routingPool.OperationsEventHandler;
			
			return service;
		}

		private static ServiceParams BuildBaseParams(string connectionString, bool isAutoSize = false, int? maxPoolSize = null,
			PoolParams? poolParams = null, ConnectionParams? connectionParams = null,
			CachingParams? cachingParams = null)
		{
			var parameters =
				new ServiceParams
				{
					ConnectionParams = connectionParams ?? new ConnectionParams { ConnectionString = connectionString },
					PoolParams = poolParams ?? new PoolParams { IsAutoPoolSize = isAutoSize, PoolSize = maxPoolSize }
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
