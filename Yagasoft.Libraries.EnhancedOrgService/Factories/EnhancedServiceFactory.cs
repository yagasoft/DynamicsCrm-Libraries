﻿#region Imports

using System;
using System.Linq;
using System.Runtime.Caching;
using System.ServiceModel;
using System.Text.RegularExpressions;
using Yagasoft.Libraries.Common;
using Yagasoft.Libraries.EnhancedOrgService.Cache;
using Yagasoft.Libraries.EnhancedOrgService.Exceptions;
using Yagasoft.Libraries.EnhancedOrgService.Helpers;
using Yagasoft.Libraries.EnhancedOrgService.Params;
using Yagasoft.Libraries.EnhancedOrgService.Services;
using Yagasoft.Libraries.EnhancedOrgService.Transactions;
using Microsoft.Xrm.Client.Caching;
using Microsoft.Xrm.Client.Services;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Tooling.Connector;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Factories
{
	/// <inheritdoc cref="IEnhancedServiceFactory{TEnhancedOrgService}" />
	public class EnhancedServiceFactory<TEnhancedOrgService> : IEnhancedServiceFactory<TEnhancedOrgService>
		where TEnhancedOrgService : EnhancedOrgServiceBase
	{
		private readonly EnhancedServiceParams parameters;
		private readonly ObjectCache factoryCache;
		private readonly Func<string, IOrganizationService> customServiceFactory = ConnectionHelpers.CreateCrmService;
		private CrmServiceClient serviceCloneBase;

		public EnhancedServiceFactory(EnhancedServiceParams parameters)
		{
			parameters.Require(nameof(parameters));

			SetPerformanceParams(parameters);

			this.parameters = parameters;

			if (parameters.IsCachingEnabled)
			{
				switch (parameters.CachingParams.CacheMode)
				{
					case CacheMode.Global:
						factoryCache = MemoryCache.Default;
						break;

					case CacheMode.Private:
						factoryCache = parameters.CachingParams.ObjectCache
							?? new MemoryCache(parameters.ConnectionParams.ConnectionString);
						break;

					case CacheMode.PrivatePerInstance:
						break;

					default:
						throw new ArgumentOutOfRangeException(nameof(parameters.CachingParams.CacheMode));
				}
			}

			var isAsyncService = typeof(IAsyncOrgService).IsAssignableFrom(typeof(TEnhancedOrgService));

			if (parameters.IsConcurrencyEnabled && !isAsyncService)
			{
				throw new UnsupportedException("Cannot create an async service factory unless the given service is async.");
			}

			if (!parameters.IsConcurrencyEnabled && isAsyncService)
			{
				throw new UnsupportedException("Cannot create an async service factory unless concurrency is enabled.");
			}

			customServiceFactory = parameters.ConnectionParams.CustomIOrgSvcFactory ?? customServiceFactory;
		}

		private static void SetPerformanceParams(EnhancedServiceParams parameters)
		{
			if (parameters.ConnectionParams?.DotNetDefaultConnectionLimit.HasValue == true)
			{
				System.Net.ServicePointManager.DefaultConnectionLimit = parameters.ConnectionParams.DotNetDefaultConnectionLimit.Value;
			}

			if (parameters.PoolParams?.DotNetSetMinAppReservedThreads.HasValue == true)
			{
				var minThreads = parameters.PoolParams.DotNetSetMinAppReservedThreads.Value;
				System.Threading.ThreadPool.SetMinThreads(minThreads, minThreads);
			}

			if (parameters.ConnectionParams?.IsDotNetDisableWaitForConnectConfirm.HasValue == true)
			{
				System.Net.ServicePointManager.Expect100Continue =
					!parameters.ConnectionParams.IsDotNetDisableWaitForConnectConfirm.Value;
			}

			if (parameters.ConnectionParams?.IsDotNetDisableNagleAlgorithm.HasValue == true)
			{
				System.Net.ServicePointManager.UseNagleAlgorithm = !parameters.ConnectionParams.IsDotNetDisableNagleAlgorithm.Value;
			}
		}

		public virtual TEnhancedOrgService CreateEnhancedService(int threads = 1)
		{
			return CreateEnhancedServiceInternal(true, threads);
		}

		internal virtual TEnhancedOrgService CreateEnhancedServiceInternal(bool isInitialiseCrmServices = true, int threads = 1)
		{
			var enhancedService = (TEnhancedOrgService)Activator.CreateInstance(typeof(TEnhancedOrgService), parameters);

			if (parameters.IsTransactionsEnabled)
			{
				enhancedService.TransactionManager = new TransactionManager();
			}

			if (parameters.IsCachingEnabled)
			{
				ObjectCache cache;

				switch (parameters.CachingParams.CacheMode)
				{
					case CacheMode.Global:
					case CacheMode.Private:
						cache = factoryCache;
						break;

					case CacheMode.PrivatePerInstance:
						cache = new MemoryCache(parameters.ConnectionParams.ConnectionString);
						break;

					default:
						throw new ArgumentOutOfRangeException(nameof(parameters.CachingParams.CacheMode));
				}

				var cacheSettings =
					new OrganizationServiceCacheSettings
					{
						PolicyFactory = new CacheItemPolicyFactory(parameters.CachingParams.Offset,
							parameters.CachingParams.SlidingExpiration)
					};

				enhancedService.Cache = new OrganizationServiceCache(cache, cacheSettings);
				enhancedService.ObjectCache = cache;
			}

			if (isInitialiseCrmServices)
			{
				enhancedService.FillServicesQueue(Enumerable.Range(0, threads).Select(e => CreateCrmService()));
			}

			return enhancedService;
		}

		public IOrganizationService CreateCrmService()
		{
			IOrganizationService service = null;

			if (serviceCloneBase == null)
			{
				service = customServiceFactory(parameters.ConnectionParams.ConnectionString);
				serviceCloneBase = service as CrmServiceClient;
			}

			if (serviceCloneBase != null)
			{
				service = serviceCloneBase.Clone();
			}
			
			if (service.EnsureTokenValid(parameters.PoolParams.TokenExpiryCheckSecs ?? 0) == false)
			{
				service = null;
			}

			return service ?? customServiceFactory(parameters.ConnectionParams.ConnectionString);
		}

		/// <summary>
		///     Clears the memory cache on the level of the factory and any services created.
		/// </summary>
		public void ClearCache()
		{
			if (!parameters.IsCachingEnabled)
			{
				throw new UnsupportedException("Cannot clear the cache because caching is not enabled.");
			}

			if (factoryCache == null)
			{
				throw new UnsupportedException("Cache is scoped to service instances."
					+ " Use each instance's method to clear the cache instead");
			}

			factoryCache.Clear();
		}
	}
}
