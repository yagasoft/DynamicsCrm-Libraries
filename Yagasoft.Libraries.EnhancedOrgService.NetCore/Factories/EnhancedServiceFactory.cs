#region Imports

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Caching;
using Microsoft.Xrm.Client.Caching;
using Microsoft.Xrm.Client.Services;
using Microsoft.Xrm.Sdk;
using Yagasoft.Libraries.Common;
using Yagasoft.Libraries.EnhancedOrgService.Cache;
using Yagasoft.Libraries.EnhancedOrgService.Helpers;
using Yagasoft.Libraries.EnhancedOrgService.Operations;
using Yagasoft.Libraries.EnhancedOrgService.Params;
using Yagasoft.Libraries.EnhancedOrgService.Pools;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced.Cache;
using Yagasoft.Libraries.EnhancedOrgService.Transactions;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Factories
{
	/// <inheritdoc cref="IEnhancedServiceFactory{TEnhancedOrgService}" />
	public class EnhancedServiceFactory<TService, TEnhancedOrgService> : IEnhancedServiceFactory<TService>
		where TService : IEnhancedOrgService
		where TEnhancedOrgService : EnhancedOrgServiceBase, TService
	{
		public ServiceParams ServiceParams { get; }

		public IOperationStats Stats { get; }

		public virtual IEnumerable<IOperationStats> StatTargets => statServices;

		private readonly ServiceFactory crmFactory;

		private readonly ObjectCache factoryCache;
		private readonly List<ObjectCache> customFactoryCaches = new();

		private readonly Func<IServiceFactory<IOrganizationService>, ServiceParams, IOrganizationService, ObjectCache>
			customCacheFactory;

		private readonly HashSet<IOperationStats> statServices = new();

		public EnhancedServiceFactory(ServiceParams parameters)
		{
			Stats = new OperationStats(this);

			var enhancedOrgType = typeof(TEnhancedOrgService);

			if (enhancedOrgType.IsAbstract)
			{
				throw new NotSupportedException("Given Enhanced Org type must be concrete.");
			}

			parameters.Require(nameof(parameters));
			parameters.ConnectionParams.Require(nameof(parameters.ConnectionParams));

			var isCachingService = typeof(ICachingOrgService).IsAssignableFrom(typeof(TEnhancedOrgService));

			if (parameters.IsCachingEnabled == true && !isCachingService)
			{
				throw new NotSupportedException("Cannot create a caching service factory unless the given service is caching.");
			}

			ParamHelpers.SetPerformanceParams(parameters.ConnectionParams);

			parameters.IsLocked = true;
			ServiceParams = parameters;

			if (parameters.IsCachingEnabled == true && parameters.CachingParams != null)
			{
				customCacheFactory = parameters.CachingParams.CustomCacheFactory;

				switch (parameters.CachingParams.CacheScope)
				{
					case CacheScope.Global:
						factoryCache = customCacheFactory == null
							? MemoryCache.Default
							: customCacheFactory((IServiceFactory<IOrganizationService>)this, ServiceParams, null);
						break;

					case CacheScope.Factory:
						factoryCache = customCacheFactory == null
							? (parameters.CachingParams.ObjectCache ?? new MemoryCache(parameters.ConnectionParams.ConnectionString))
							: customCacheFactory((IServiceFactory<IOrganizationService>)this, ServiceParams, null);
						break;

					case CacheScope.Service:
						break;

					default:
						throw new ArgumentOutOfRangeException(nameof(parameters.CachingParams.CacheScope));
				}
			}

			crmFactory = new ServiceFactory(parameters);
		}

		public virtual async Task<TService> CreateService()
		{
			var service = CreateEnhancedService();
			(service as TEnhancedOrgService)?.InitialiseConnection(await crmFactory.CreateService());
			return service;
		}

		public virtual TService CreateService(IServicePool<IOrganizationService> servicePool)
		{
			servicePool.Require(nameof(servicePool));
			var service = CreateEnhancedService();
			(service as TEnhancedOrgService)?.InitialiseConnection(servicePool);
			return service;
		}

		public void ClearCache()
		{
			if (ServiceParams.IsCachingEnabled != true)
			{
				throw new NotSupportedException("Cannot clear the cache because caching is not enabled.");
			}

			if (ServiceParams.CachingParams?.CacheScope == CacheScope.Service)
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

		internal virtual TService CreateEnhancedService()
		{
			var enhancedService = (TEnhancedOrgService)Activator.CreateInstance(typeof(TEnhancedOrgService),
				BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { ServiceParams }, null);

			if (ServiceParams.IsTransactionsEnabled == true)
			{
				enhancedService.TransactionManager = new TransactionManager();
			}

			if (ServiceParams.IsCachingEnabled == true)
			{
				InitialiseCaching(enhancedService);
			}

			statServices.Add(enhancedService);

			(Stats as OperationStats)?.Propagate();

			return enhancedService;
		}

		private void InitialiseCaching(TEnhancedOrgService enhancedService)
		{
			if (ServiceParams.IsCachingEnabled != true || ServiceParams.CachingParams == null)
			{
				return;
			}

			enhancedService.Require(nameof(enhancedService));

			ObjectCache cache;

			switch (ServiceParams.CachingParams.CacheScope)
			{
				case CacheScope.Global:
				case CacheScope.Factory:
					cache = factoryCache;
					break;

				case CacheScope.Service:
					cache = customCacheFactory == null
						? new MemoryCache(ServiceParams.ConnectionParams.ConnectionString)
						: customCacheFactory((IServiceFactory<IOrganizationService>)this, ServiceParams, enhancedService);
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(ServiceParams.CachingParams.CacheScope));
			}

			OrganizationServiceCacheSettings cacheSettings = null;

			if (ServiceParams.CachingParams.Offset.HasValue)
			{
				cacheSettings =
					new OrganizationServiceCacheSettings
					{
						PolicyFactory = new CacheItemPolicyFactory(ServiceParams.CachingParams.Offset.Value, ServiceParams.CachingParams.Priority)
					};
			}

			if (ServiceParams.CachingParams.SlidingExpiration.HasValue)
			{
				cacheSettings =
					new OrganizationServiceCacheSettings
					{
						PolicyFactory = new CacheItemPolicyFactory(ServiceParams.CachingParams.SlidingExpiration.Value,
							ServiceParams.CachingParams.Priority)
					};
			}

			enhancedService.Cache = new OrganizationServiceCache(cache, cacheSettings);
			enhancedService.ObjectCache = cache;
		}
	}
}
