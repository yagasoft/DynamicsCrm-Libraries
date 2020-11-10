#region Imports

using System;
using System.Collections.Generic;
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
using Yagasoft.Libraries.EnhancedOrgService.Operations;
using Yagasoft.Libraries.EnhancedOrgService.Params;
using Yagasoft.Libraries.EnhancedOrgService.Response.Operations;
using Yagasoft.Libraries.EnhancedOrgService.Router;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced.Balancing;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced.Cache;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced.Transactions;
using Yagasoft.Libraries.EnhancedOrgService.Transactions;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Factories
{
	/// <inheritdoc cref="IEnhancedServiceFactory{TEnhancedOrgService}" />
	public class EnhancedServiceFactory<TServiceInterface, TEnhancedOrgService> : IEnhancedServiceFactory<TServiceInterface>
		where TServiceInterface : IEnhancedOrgService
		where TEnhancedOrgService : EnhancedOrgServiceBase, TServiceInterface
	{
		public IOperationStats Stats { get; }

		public virtual IEnumerable<IOperationStats> StatTargets => statServices;

		internal readonly EnhancedServiceParams Parameters;

		private readonly ObjectCache factoryCache;
		private readonly List<ObjectCache> customFactoryCaches = new List<ObjectCache>();
		private readonly Func<IServiceFactory, EnhancedServiceParams, IEnhancedOrgService, ObjectCache> customCacheFactory;

		private readonly Func<string, IOrganizationService> customServiceFactory = ConnectionHelpers.CreateCrmService;
		private CrmServiceClient serviceCloneBase;

		private readonly HashSet<IOperationStats> statServices = new HashSet<IOperationStats>();

		private readonly IRoutingService routeringService;

		public EnhancedServiceFactory(IRoutingService routeringService)
		{
			Stats = new OperationStats(this);

			this.routeringService = routeringService;
			var enhancedOrgType = typeof(TEnhancedOrgService);

			if (enhancedOrgType.IsAbstract)
			{
				throw new NotSupportedException("Given Enhanced Org type must be concrete.");
			}

			if (!typeof(ISelfBalancingOrgService).IsAssignableFrom(typeof(TServiceInterface)))
			{
				throw new NotSupportedException("Given Enhanced Org interface must be self-balancing.");
			}

			routeringService.Require(nameof(routeringService));
		}

		public EnhancedServiceFactory(EnhancedServiceParams parameters)
		{
			Stats = new OperationStats(this);

			var enhancedOrgType = typeof(TEnhancedOrgService);

			if (enhancedOrgType.IsAbstract)
			{
				throw new NotSupportedException("Given Enhanced Org type must be concrete.");
			}

			parameters.Require(nameof(parameters));

			var isCachingService = typeof(ICachingOrgService).IsAssignableFrom(typeof(TEnhancedOrgService));

			if (parameters.IsCachingEnabled == true && !isCachingService)
			{
				throw new NotSupportedException("Cannot create a caching service factory unless the given service is caching.");
			}

			SetPerformanceParams(parameters);

			parameters.IsLocked = true;
			Parameters = parameters;

			if (parameters.IsCachingEnabled == true && parameters.CachingParams != null)
			{
				customCacheFactory = parameters.CachingParams.CustomCacheFactory;

				switch (parameters.CachingParams.CacheScope)
				{
					case CacheScope.Global:
						factoryCache = customCacheFactory == null ? MemoryCache.Default : customCacheFactory(this, Parameters, null);
						break;

					case CacheScope.Factory:
						factoryCache = customCacheFactory == null
							? (parameters.CachingParams.ObjectCache ?? new MemoryCache(parameters.ConnectionParams.ConnectionString))
							: customCacheFactory(this, Parameters, null);
						break;

					case CacheScope.Service:
						break;

					default:
						throw new ArgumentOutOfRangeException(nameof(parameters.CachingParams.CacheScope));
				}
			}

			customServiceFactory = parameters.ConnectionParams.CustomIOrgSvcFactory ?? customServiceFactory;
		}

		public virtual TServiceInterface CreateEnhancedService(int threads = 1)
		{
			threads.RequireAtLeast(1);

			if (routeringService != null)
			{
				if (threads > 1)
				{
					throw new NotSupportedException($"Self-balancing services don't require the '{nameof(threads)}' parameter to be defined.");
				}
			}

			return CreateEnhancedServiceInternal(true, threads);
		}

		public IOrganizationService CreateCrmService()
		{
			IOrganizationService service = null;

			if (serviceCloneBase == null)
			{
				service = customServiceFactory(Parameters.ConnectionParams.ConnectionString);
				serviceCloneBase = service as CrmServiceClient;
			}

			if (serviceCloneBase != null)
			{
				service = serviceCloneBase.Clone();
			}

			if (service.EnsureTokenValid(Parameters.PoolParams?.TokenExpiryCheckSecs ?? 0) == false)
			{
				service = null;
			}

			return service ?? customServiceFactory(Parameters.ConnectionParams.ConnectionString);
		}

		internal virtual TServiceInterface CreateEnhancedServiceInternal(bool isInitialiseCrmServices = true, int threads = 1)
		{
			if (routeringService != null)
			{
				var balancingService = (TEnhancedOrgService)Activator.CreateInstance(typeof(TEnhancedOrgService),
					BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { routeringService }, null);

				return balancingService;
				// TODO propagate events
			}

			var enhancedService = (TEnhancedOrgService)Activator.CreateInstance(typeof(TEnhancedOrgService),
				BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { Parameters }, null);

			if (Parameters.IsTransactionsEnabled == true)
			{
				enhancedService.TransactionManager = new TransactionManager();
			}

			if (Parameters.IsCachingEnabled == true)
			{
				InitialiseCaching(enhancedService);
			}

			if (isInitialiseCrmServices)
			{
				enhancedService.FillServicesQueue(Enumerable.Range(0, threads).Select(e => CreateCrmService()));
			}

			statServices.Add(enhancedService);

			(Stats as OperationStats)?.Propagate();

			return enhancedService;
		}

		private void InitialiseCaching(TEnhancedOrgService enhancedService)
		{
			if (Parameters.IsCachingEnabled != true || Parameters.CachingParams == null)
			{
				return;
			}

			ObjectCache cache;

			switch (Parameters.CachingParams.CacheScope)
			{
				case CacheScope.Global:
				case CacheScope.Factory:
					cache = factoryCache;
					break;

				case CacheScope.Service:
					cache = customCacheFactory == null
						? new MemoryCache(Parameters.ConnectionParams.ConnectionString)
						: customCacheFactory(this, Parameters, enhancedService);
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(Parameters.CachingParams.CacheScope));
			}

			OrganizationServiceCacheSettings cacheSettings = null;

			if (Parameters.CachingParams.Offset.HasValue)
			{
				cacheSettings =
					new OrganizationServiceCacheSettings
					{
						PolicyFactory = new CacheItemPolicyFactory(Parameters.CachingParams.Offset.Value, Parameters.CachingParams.Priority)
					};
			}

			if (Parameters.CachingParams.SlidingExpiration.HasValue)
			{
				cacheSettings =
					new OrganizationServiceCacheSettings
					{
						PolicyFactory = new CacheItemPolicyFactory(Parameters.CachingParams.SlidingExpiration.Value,
							Parameters.CachingParams.Priority)
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
			if (Parameters.IsCachingEnabled != true)
			{
				throw new NotSupportedException("Cannot clear the cache because caching is not enabled.");
			}

			if (Parameters.CachingParams?.CacheScope == CacheScope.Service)
			{
				foreach (var service in statServices.OfType<ICachingOrgService>())
				{
					service.ClearCache();
				}
			}

			factoryCache?.Clear();

			foreach (var cache in customFactoryCaches)
			{
				cache?.Clear();
			}
		}
	}
}
