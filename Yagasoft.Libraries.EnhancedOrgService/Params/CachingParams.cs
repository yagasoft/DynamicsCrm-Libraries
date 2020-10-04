#region Imports

using System;
using System.Runtime.Caching;
using Yagasoft.Libraries.Common;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Params
{
	public enum CacheMode
	{
		/// <summary>
		///     Will use ID shared with all connections<br />
		/// </summary>
		Global,

		/// <summary>
		///     Will use own ID isolated from all others
		/// </summary>
		Private,

		/// <summary>
		///     Each connection will have its own ID isolated from all others
		/// </summary>
		PrivatePerInstance
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
		///     Default: Private
		/// </summary>
		public CacheMode? CacheMode
		{
			get => cacheMode ?? Params.CacheMode.Private;
			set
			{
				ValidateLock();
				value.Require(nameof(CacheMode));
				cacheMode = value;
			}
		}

		/// <summary>
		///     The time after which to remove the object from the cache in seconds. Not affected by the sliding expiration.<br />
		///     Default: infinite<br />
		///     You can't use an offset with a sliding expiration together ('offset' will take precedence)
		/// </summary>
		public double? Offset
		{
			get => offset;
			set
			{
				ValidateLock();
				value.Require(nameof(Offset));
				offset = value;
			}
		}

		/// <summary>
		///     The duration after which to remove the object from cache, if it was not accessed for that duration.<br />
		///     You can't use an offset with a sliding expiration together ('offset' will take precedence)
		/// </summary>
		public TimeSpan? SlidingExpiration
		{
			get => slidingExpiration;
			set
			{
				ValidateLock();
				value.Require(nameof(SlidingExpiration));
				slidingExpiration = value;
			}
		}

		/// <summary>
		///     The priority of keeping the item in the cache if it becomes filled.<br />
		///     Default: Default
		/// </summary>
		public CacheItemPriority? Priority
		{
			get => priority;
			set
			{
				ValidateLock();
				value.Require(nameof(Priority));
				priority = value;
			}
		}

		private ObjectCache objectCache;
		private CacheMode? cacheMode;
		private double? offset;
		private TimeSpan? slidingExpiration;
		private CacheItemPriority? priority;
	}
}
