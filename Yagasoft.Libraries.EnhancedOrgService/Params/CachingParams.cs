#region Imports

using System;
using System.Runtime.Caching;
using Microsoft.Xrm.Sdk;
using Yagasoft.Libraries.Common;
using Yagasoft.Libraries.EnhancedOrgService.Factories;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced.Transactions;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Params
{
	public enum CacheScope
	{
		/// <summary>
		///     Cache shared between all services.
		/// </summary>
		Global,

		/// <summary>
		///     Cache limited to services within a factory.
		/// </summary>
		Factory,

		/// <summary>
		///     Each service will have its own cache, isolated from all others.
		/// </summary>
		Service
	}

	public class CachingParams : ParamsBase
	{
		/// <summary>
		///     If 'null', will use the default memory cache.
		/// </summary>
		public ObjectCache ObjectCache
		{
			get => objectCache;
			set
			{
				ValidateLock();
				objectCache = value;
			}
		}

		/// <summary>
		///     The mode determines the extra string added to the key to uniquely identify each query.<br />
		///     If an ID is shared, then a query run from different services, will have the same result from the cache.<br />
		///     Default: Factory.
		/// </summary>
		public CacheScope CacheScope
		{
			get => cacheMode ?? CacheScope.Factory;
			set
			{
				ValidateLock();
				cacheMode = value;
			}
		}

		/// <summary>
		///     The time after which to remove the object from the cache in seconds. Not affected by the sliding expiration.<br />
		///     Default: infinite.<br />
		///     You can't use an offset with a sliding expiration together ('offset' will take precedence).
		/// </summary>
		public double? Offset
		{
			get => offset;
			set
			{
				ValidateLock();
				value?.RequireAtLeast(0, nameof(Offset));
				offset = value;
			}
		}

		/// <summary>
		///     The duration after which to remove the object from cache, if it was not accessed for that duration.<br />
		///     You can't use an offset with a sliding expiration together ('offset' will take precedence).
		/// </summary>
		public TimeSpan? SlidingExpiration
		{
			get => slidingExpiration;
			set
			{
				ValidateLock();
				value?.TotalMilliseconds.RequireAtLeast(0, nameof(SlidingExpiration));
				slidingExpiration = value;
			}
		}

		/// <summary>
		///     The priority of keeping the item in the cache if it becomes filled.<br />
		///     Default: Default.
		/// </summary>
		public CacheItemPriority? Priority
		{
			get => priority;
			set
			{
				ValidateLock();
				priority = value;
			}
		}

		/// <summary>
		///     Custom cache to use instead of <see cref="MemoryCache"/> or the <see cref="ObjectCache"/>.
		/// </summary>
		public Func<IServiceFactory<IOrganizationService>, ServiceParams, IOrganizationService, ObjectCache> CustomCacheFactory
		{
			get => customCacheFactory;
			set
			{
				ValidateLock();
				customCacheFactory = value;
			}
		}

		private ObjectCache objectCache;
		private CacheScope? cacheMode;
		private double? offset;
		private TimeSpan? slidingExpiration;
		private CacheItemPriority? priority;
		private Func<IServiceFactory<IOrganizationService>, ServiceParams, IOrganizationService, ObjectCache> customCacheFactory;
	}
}
