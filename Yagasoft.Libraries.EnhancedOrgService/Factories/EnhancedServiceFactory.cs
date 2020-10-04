#region Imports

using System;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Caching;
using System.Threading;
using Microsoft.Xrm.Client.Caching;
using Microsoft.Xrm.Client.Services;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Tooling.Connector;
using Yagasoft.Libraries.Common;
using Yagasoft.Libraries.EnhancedOrgService.Cache;
using Yagasoft.Libraries.EnhancedOrgService.Helpers;
using Yagasoft.Libraries.EnhancedOrgService.Params;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced.Async;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced.Cache;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced.Transactions;
using Yagasoft.Libraries.EnhancedOrgService.Transactions;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Factories
{
	/// <inheritdoc cref="IEnhancedServiceFactory{TEnhancedOrgService}" />
	public class EnhancedServiceFactory<TServiceInterface, TEnhancedOrgService> : IEnhancedServiceFactory<TServiceInterface>
		where TServiceInterface : ITransactionOrgService
		where TEnhancedOrgService : EnhancedOrgServiceBase, TServiceInterface
	{
		private readonly EnhancedServiceParams parameters;
		private readonly ObjectCache factoryCache;
		private readonly Func<string, IOrganizationService> customServiceFactory = ConnectionHelpers.CreateCrmService;
		private CrmServiceClient serviceCloneBase;

		public EnhancedServiceFactory(EnhancedServiceParams parameters)
		{
			var enhancedOrgType = typeof(TEnhancedOrgService);

			if (enhancedOrgType.IsAbstract)
			{
				throw new NotSupportedException("Given Enhanced Org type must be concrete.");
			}

			parameters.Require(nameof(parameters));

			var isCachingService = typeof(ICachingOrgService).IsAssignableFrom(typeof(TEnhancedOrgService));

			if (parameters.IsCachingEnabled && !isCachingService)
			{
				throw new NotSupportedException("Cannot create a caching service factory unless the given service is caching.");
			}

			var isAsyncService = typeof(IAsyncOrgService).IsAssignableFrom(typeof(TEnhancedOrgService));

			if (parameters.IsConcurrencyEnabled && !isAsyncService)
			{
				throw new NotSupportedException("Cannot create an async service factory unless the given service is async.");
			}

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

			customServiceFactory = parameters.ConnectionParams.CustomIOrgSvcFactory ?? customServiceFactory;
		}

		public virtual TServiceInterface CreateEnhancedService(int threads = 1)
		{
			return CreateEnhancedServiceInternal(true, threads);
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

		internal virtual TServiceInterface CreateEnhancedServiceInternal(bool isInitialiseCrmServices = true, int threads = 1)
		{
			var enhancedService = (TEnhancedOrgService)Activator.CreateInstance(typeof(TEnhancedOrgService),
				BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { parameters }, null);

			if (parameters.IsTransactionsEnabled)
			{
				enhancedService.TransactionManager = new TransactionManager();
			}

			if (parameters.IsCachingEnabled)
			{
				InitialiseCaching(enhancedService);
			}

			if (isInitialiseCrmServices)
			{
				enhancedService.FillServicesQueue(Enumerable.Range(0, threads).Select(e => CreateCrmService()));
			}

			return enhancedService;
		}

		private void InitialiseCaching(TEnhancedOrgService enhancedService)
		{
			if (!parameters.IsCachingEnabled)
			{
				return;
			}

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

			OrganizationServiceCacheSettings cacheSettings = null;

			if (parameters.CachingParams.Offset.HasValue)
			{
				cacheSettings =
					new OrganizationServiceCacheSettings
					{
						PolicyFactory = new CacheItemPolicyFactory(parameters.CachingParams.Offset.Value, parameters.CachingParams.Priority)
					};
			}

			if (parameters.CachingParams.SlidingExpiration.HasValue)
			{
				cacheSettings =
					new OrganizationServiceCacheSettings
					{
						PolicyFactory = new CacheItemPolicyFactory(parameters.CachingParams.SlidingExpiration.Value,
							parameters.CachingParams.Priority)
					};
			}

			enhancedService.Cache = new OrganizationServiceCache(cache, cacheSettings);
			enhancedService.ObjectCache = cache;
		}

		private static void SetPerformanceParams(EnhancedServiceParams parameters)
		{
			if (parameters.ConnectionParams?.DotNetDefaultConnectionLimit.HasValue == true)
			{
				ServicePointManager.DefaultConnectionLimit = parameters.ConnectionParams.DotNetDefaultConnectionLimit.Value;
			}

			if (parameters.PoolParams?.DotNetSetMinAppReservedThreads.HasValue == true)
			{
				var minThreads = parameters.PoolParams.DotNetSetMinAppReservedThreads.Value;
				ThreadPool.SetMinThreads(minThreads, minThreads);
			}

			if (parameters.ConnectionParams?.IsDotNetDisableWaitForConnectConfirm.HasValue == true)
			{
				ServicePointManager.Expect100Continue =
					!parameters.ConnectionParams.IsDotNetDisableWaitForConnectConfirm.Value;
			}

			if (parameters.ConnectionParams?.IsDotNetDisableNagleAlgorithm.HasValue == true)
			{
				ServicePointManager.UseNagleAlgorithm = !parameters.ConnectionParams.IsDotNetDisableNagleAlgorithm.Value;
			}
		}

		/// <summary>
		///     Clears the memory cache on the level of the factory and any services created.
		/// </summary>
		public void ClearCache()
		{
			if (!parameters.IsCachingEnabled)
			{
				throw new NotSupportedException("Cannot clear the cache because caching is not enabled.");
			}

			if (factoryCache == null)
			{
				throw new NotSupportedException("Cache is scoped to service instances."
					+ " Use each instance's method to clear the cache instead");
			}

			factoryCache.Clear();
		}
	}
}
