#region Imports

using System.Collections.Generic;
using System.Threading.Tasks;
using Yagasoft.Libraries.EnhancedOrgService.Factories;
using Yagasoft.Libraries.EnhancedOrgService.Params;
using Yagasoft.Libraries.EnhancedOrgService.Pools;
using Yagasoft.Libraries.EnhancedOrgService.Router;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced.Balancing;
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
		///     <inheritdoc cref="EnhancedServiceParams.AutoSetMaxPerformanceParams" /><br />
		///     <b>This setting is applied if 'true', and only right after creating the next pool.</b>
		/// </summary>
		public static bool AutoSetMaxPerformanceParams { get; set; }

		public static IEnhancedServicePool<IEnhancedOrgService> GetPool(EnhancedServiceParams serviceParams)
		{
			var factory = new EnhancedServiceFactory<IEnhancedOrgService, Services.Enhanced.EnhancedOrgService>(serviceParams);
			return new EnhancedServicePool<IEnhancedOrgService, Services.Enhanced.EnhancedOrgService>(factory);
		}

		public static IEnhancedServicePool<ICachingOrgService> GetPoolCaching(EnhancedServiceParams serviceParams)
		{
			var factory = new EnhancedServiceFactory<ICachingOrgService, CachingOrgService>(serviceParams);
			return new EnhancedServicePool<ICachingOrgService, CachingOrgService>(factory);
		}

		public static IEnhancedServicePool<IEnhancedOrgService> GetPool(string connectionString, int poolSize = 2)
		{
			var parameters = BuildBaseParams(connectionString, poolSize);
			var factory = new EnhancedServiceFactory<IEnhancedOrgService, Services.Enhanced.EnhancedOrgService>(parameters);
			return new EnhancedServicePool<IEnhancedOrgService, Services.Enhanced.EnhancedOrgService>(factory, poolSize);
		}

		public static IEnhancedServicePool<ICachingOrgService> GetPoolCaching(string connectionString, int poolSize = 2,
			CachingParams cachingParams = null)
		{
			var parameters = BuildBaseParams(connectionString, poolSize, null,
				cachingParams: cachingParams ?? new CachingParams());
			var factory = new EnhancedServiceFactory<ICachingOrgService, CachingOrgService>(parameters);
			return new EnhancedServicePool<ICachingOrgService, CachingOrgService>(factory, poolSize);
		}

		public static IEnhancedServicePool<IEnhancedOrgService> GetPool(string connectionString, PoolParams poolParams)
		{
			var parameters = BuildBaseParams(connectionString, 2, poolParams);
			var factory = new EnhancedServiceFactory<IEnhancedOrgService, Services.Enhanced.EnhancedOrgService>(parameters);
			return new EnhancedServicePool<IEnhancedOrgService, Services.Enhanced.EnhancedOrgService>(factory, poolParams);
		}

		public static IEnhancedServicePool<ICachingOrgService> GetPoolCaching(string connectionString, PoolParams poolParams,
			CachingParams cachingParams = null)
		{
			var parameters = BuildBaseParams(connectionString, 2, null,
				cachingParams: cachingParams ?? new CachingParams());
			var factory = new EnhancedServiceFactory<ICachingOrgService, CachingOrgService>(parameters);
			return new EnhancedServicePool<ICachingOrgService, CachingOrgService>(factory, poolParams);
		}

		public static IEnhancedServicePool<IEnhancedOrgService> GetPool(ConnectionParams connectionParams, PoolParams poolParams)
		{
			var parameters = BuildBaseParams(string.Empty, 2, poolParams, connectionParams);
			var factory = new EnhancedServiceFactory<IEnhancedOrgService, Services.Enhanced.EnhancedOrgService>(parameters);
			return new EnhancedServicePool<IEnhancedOrgService, Services.Enhanced.EnhancedOrgService>(factory, poolParams);
		}

		public static IEnhancedServicePool<ICachingOrgService> GetPoolCaching(ConnectionParams connectionParams, PoolParams poolParams,
			CachingParams cachingParams = null)
		{
			var parameters = BuildBaseParams(string.Empty, 2, null, connectionParams,
				cachingParams ?? new CachingParams());
			var factory = new EnhancedServiceFactory<ICachingOrgService, CachingOrgService>(parameters);
			return new EnhancedServicePool<ICachingOrgService, CachingOrgService>(factory, poolParams);
		}

		public static async Task<ISelfBalancingOrgService> GetSelfBalancingService(IEnumerable<EnhancedServiceParams> nodeParameters, RouterRules rules)
		{
			var routingService = new RoutingService();

			foreach (var parameters in nodeParameters)
			{
				routingService.AddNode(parameters);
			}

			routingService.DefineRules(rules);
			await routingService.StartRouter();

			return new EnhancedServiceFactory<ISelfBalancingOrgService, SelfBalancingOrgService>(routingService).CreateEnhancedService();
		}

		private static EnhancedServiceParams BuildBaseParams(string connectionString, int poolSize,
			PoolParams poolParams = null, ConnectionParams connectionParams = null,
			CachingParams cachingParams = null)
		{
			var parameters =
				new EnhancedServiceParams
				{
					ConnectionParams = connectionParams ?? new ConnectionParams { ConnectionString = connectionString },
					PoolParams = poolParams ?? new PoolParams { PoolSize = poolSize }
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
