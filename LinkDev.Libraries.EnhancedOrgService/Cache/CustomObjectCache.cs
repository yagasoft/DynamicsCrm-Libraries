using System;
using System.Web;
using System.Runtime.Caching;

namespace CustomCacheSample
{
	public class CustomCache : MemoryCache
	{
		public CustomCache() : base("defaultCustomCache") { }

		public override void Set(CacheItem item, CacheItemPolicy policy) => 
			Set(item.Key, item.Value, policy, item.RegionName);

		public override void Set(string key, object value, DateTimeOffset absoluteExpiration, string regionName = null) => 
			Set(key, value, new CacheItemPolicy { AbsoluteExpiration = absoluteExpiration }, regionName);

		public override void Set(string key, object value, CacheItemPolicy policy, string regionName = null) => 
			base.Set(CreateKeyWithRegion(key, regionName), value, policy);

		public override CacheItem GetCacheItem(string key, string regionName = null)
		{
			var temporary = base.GetCacheItem(CreateKeyWithRegion(key, regionName));
			return new CacheItem(key, temporary.Value, regionName);
		}

		public override object Get(string key, string regionName = null) => 
			base.Get(CreateKeyWithRegion(key, regionName));

		public override DefaultCacheCapabilities DefaultCacheCapabilities =>
			(base.DefaultCacheCapabilities | DefaultCacheCapabilities.CacheRegions);

		private string CreateKeyWithRegion(string key, string region) => 
			"region:" + (region == null ? "null_region" : region) + ";key=" + key;
	}
}