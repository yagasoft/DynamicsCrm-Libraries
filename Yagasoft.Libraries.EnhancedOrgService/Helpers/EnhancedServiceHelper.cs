#region Imports

using System;
using Microsoft.Xrm.Sdk;
using Yagasoft.Libraries.EnhancedOrgService.Factories;
using Yagasoft.Libraries.EnhancedOrgService.Params;
using Yagasoft.Libraries.EnhancedOrgService.Pools;
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
		///     <inheritdoc cref="EnhancedServiceParams.AutoSetMaxPerformanceParams" /><br />
		///     <b>Applies only when creating pools after setting this to 'true'.</b>
		/// </summary>
		public static bool AutoSetMaxPerformanceParams { get; set; }

		public static IEnhancedServicePool<IEnhancedOrgService> GetPool(string connectionString, int poolSize = 2,
			ConnectionParams connectionParams = null, TransactionParams transactionParams = null)
		{
			var parameters = BuildBaseParams(connectionString, poolSize, null, connectionParams, transactionParams);
			var factory = new EnhancedServiceFactory<IEnhancedOrgService, Services.Enhanced.EnhancedOrgService>(parameters);
			return new EnhancedServicePool<IEnhancedOrgService, Services.Enhanced.EnhancedOrgService>(factory, poolSize);
		}

		public static IEnhancedServicePool<ICachingOrgService> GetPoolCaching(string connectionString, int poolSize = 2,
			CachingParams cachingParams = null, ConnectionParams connectionParams = null, TransactionParams transactionParams = null)
		{
			var parameters = BuildBaseParams(connectionString, poolSize, null, connectionParams, transactionParams,
				cachingParams ?? new CachingParams());
			var factory = new EnhancedServiceFactory<ICachingOrgService, Services.Enhanced.EnhancedOrgService>(parameters);
			return new EnhancedServicePool<ICachingOrgService, Services.Enhanced.EnhancedOrgService>(factory, poolSize);
		}

		public static IEnhancedServicePool<IEnhancedOrgService> GetPool(string connectionString, PoolParams poolParams,
			ConnectionParams connectionParams = null, TransactionParams transactionParams = null)
		{
			var parameters = BuildBaseParams(connectionString, 2, poolParams, connectionParams, transactionParams);
			var factory = new EnhancedServiceFactory<IEnhancedOrgService, Services.Enhanced.EnhancedOrgService>(parameters);
			return new EnhancedServicePool<IEnhancedOrgService, Services.Enhanced.EnhancedOrgService>(factory, poolParams);
		}

		public static IEnhancedServicePool<ICachingOrgService> GetPoolCaching(string connectionString, PoolParams poolParams,
			CachingParams cachingParams = null, ConnectionParams connectionParams = null, TransactionParams transactionParams = null)
		{
			var parameters = BuildBaseParams(connectionString, 2, null, connectionParams, transactionParams,
				cachingParams ?? new CachingParams());
			var factory = new EnhancedServiceFactory<ICachingOrgService, Services.Enhanced.EnhancedOrgService>(parameters);
			return new EnhancedServicePool<ICachingOrgService, Services.Enhanced.EnhancedOrgService>(factory, poolParams);
		}

		public static IEnhancedServicePool<IEnhancedOrgService> GetPool(ConnectionParams connectionParams, PoolParams poolParams,
			TransactionParams transactionParams = null)
		{
			var parameters = BuildBaseParams(string.Empty, 2, poolParams, connectionParams, transactionParams);
			var factory = new EnhancedServiceFactory<IEnhancedOrgService, Services.Enhanced.EnhancedOrgService>(parameters);
			return new EnhancedServicePool<IEnhancedOrgService, Services.Enhanced.EnhancedOrgService>(factory, poolParams);
		}

		public static IEnhancedServicePool<ICachingOrgService> GetPoolCaching(ConnectionParams connectionParams, PoolParams poolParams,
			CachingParams cachingParams = null, TransactionParams transactionParams = null)
		{
			var parameters = BuildBaseParams(string.Empty, 2, null, connectionParams, transactionParams,
				cachingParams ?? new CachingParams());
			var factory = new EnhancedServiceFactory<ICachingOrgService, Services.Enhanced.EnhancedOrgService>(parameters);
			return new EnhancedServicePool<ICachingOrgService, Services.Enhanced.EnhancedOrgService>(factory, poolParams);
		}

		private static EnhancedServiceParams BuildBaseParams(string connectionString, int poolSize,
			PoolParams poolParams = null, ConnectionParams connectionParams = null,
			TransactionParams transactionParams = null, CachingParams cachingParams = null)
		{
			var parameters =
				new EnhancedServiceParams
				{
					ConnectionParams = connectionParams ?? new ConnectionParams { ConnectionString = connectionString },
					PoolParams = poolParams ?? new PoolParams { PoolSize = poolSize }
				};

			if (transactionParams != null)
			{
				parameters.IsTransactionsEnabled = true;
				parameters.TransactionParams = transactionParams;
			}

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
