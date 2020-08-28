#region Imports

using System;
using Microsoft.Xrm.Sdk;
using Yagasoft.Libraries.Common;
using Yagasoft.Libraries.EnhancedOrgService.Builders;
using Yagasoft.Libraries.EnhancedOrgService.Factories;
using Yagasoft.Libraries.EnhancedOrgService.Params;
using Yagasoft.Libraries.EnhancedOrgService.Pools;
using Yagasoft.Libraries.EnhancedOrgService.Services;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Helpers
{
	/// <summary>
	///     Provides methods to quickly and easily create Enhanced Org Services.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	public static class EnhancedServiceHelper
	{
		/// <summary>
		///     Convenience method to get a service pool.
		/// </summary>
		/// <param name="connectionString">The connection string.</param>
		/// <param name="poolSize">Size of the pool.</param>
		/// <param name="customIOrgSvcFactory">
		///     A custom factory that will be used to create CRM connections instead of the library built-in method.
		/// </param>
		public static EnhancedServicePool<Services.EnhancedOrgService> GetPool(string connectionString, int poolSize = 2,
			Func<string, IOrganizationService> customIOrgSvcFactory = null)
		{
			return BuildPool<Services.EnhancedOrgService>(
				new EnhancedServiceParams
				{
					ConnectionParams =
						new ConnectionParams
						{
							ConnectionString = connectionString,
							CustomIOrgSvcFactory = customIOrgSvcFactory
						},
					PoolParams = new PoolParams { PoolSize = poolSize }
				});
		}
		
		public static EnhancedServicePool<Services.EnhancedOrgService> GetPool(EnhancedServiceParams serviceParams)
		{
			serviceParams.Require(nameof(serviceParams));
			return BuildPool<Services.EnhancedOrgService>(serviceParams);
		}

		/// <summary>
		///     Convenience method to get an async-based service pool.
		/// </summary>
		/// <param name="connectionString">The connection string.</param>
		/// <param name="poolSize">Size of the pool.</param>
		/// <param name="customIOrgSvcFactory">
		///     A custom factory that will be used to create CRM connections instead of the library built-in method.
		/// </param>
		public static EnhancedServicePool<AsyncOrgService> GetAsyncPool(string connectionString, int poolSize = 2,
			Func<string, IOrganizationService> customIOrgSvcFactory = null)
		{
			return BuildPool<AsyncOrgService>(
				new EnhancedServiceParams
				{
					ConnectionParams =
						new ConnectionParams
						{
							ConnectionString = connectionString,
							CustomIOrgSvcFactory = customIOrgSvcFactory
						},
					PoolParams = new PoolParams { PoolSize = poolSize }
				});
		}

		public static EnhancedServicePool<AsyncOrgService> GetAsyncPool(EnhancedServiceParams serviceParams)
		{
			serviceParams.Require(nameof(serviceParams));
			return BuildPool<AsyncOrgService>(serviceParams);
		}
		
		private static EnhancedServicePool<TService> BuildPool<TService>(EnhancedServiceParams serviceParams)
			where TService : EnhancedOrgServiceBase
		{
			var builder = InitialiseBuilderConfig(serviceParams);
			var build = builder.Finalise().GetBuild();
			var factory = new EnhancedServiceFactory<TService>(build);
			return new EnhancedServicePool<TService>(factory, serviceParams.PoolParams?.PoolSize ?? 2);
		}

		private static EnhancedServiceBuilder InitialiseBuilderConfig(EnhancedServiceParams serviceParams)
		{
			serviceParams.ConnectionParams.Require(nameof(serviceParams.ConnectionParams));
			serviceParams.ConnectionParams.ConnectionString.RequireFilled(nameof(serviceParams.ConnectionParams.ConnectionString));
			
			var builder = EnhancedServiceBuilder.NewBuilder.Initialise(serviceParams.ConnectionParams.ConnectionString);

			if (serviceParams.IsCachingEnabled)
			{
				builder.AddCaching(serviceParams.CachingParams);
			}

			if (serviceParams.IsConcurrencyEnabled)
			{
				builder.AddConcurrency(serviceParams.ConcurrencyParams);

				if (serviceParams.ConcurrencyParams.IsAsyncAppHold)
				{
					builder.HoldAppForAsync();
				}
			}

			if (serviceParams.IsTransactionsEnabled)
			{
				builder.AddTransactions(serviceParams.TransactionParams);
			}

			return builder;
		}
	}
}
