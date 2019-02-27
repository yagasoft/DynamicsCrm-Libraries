using System;
using System.Runtime.Caching;
using Microsoft.Xrm.Client.Services;

namespace Yagasoft.Libraries.EnhancedOrgService.Cache
{
	internal class CacheItemPolicyFactory : ICacheItemPolicyFactory
	{
		private readonly DateTimeOffset? offset;
		private readonly TimeSpan? slidingExpiration;

		public CacheItemPolicyFactory(DateTimeOffset? offset)
		{
			this.offset = offset;
		}

		public CacheItemPolicyFactory(TimeSpan? slidingExpiration)
		{
			this.slidingExpiration = slidingExpiration;
		}

		public CacheItemPolicyFactory(DateTimeOffset? offset, TimeSpan? slidingExpiration)
		{
			this.offset = offset;
			this.slidingExpiration = slidingExpiration;
		}

		public CacheItemPolicy Create()
		{
			var policy = new CacheItemPolicy();

			if (offset.HasValue)
			{
				policy.AbsoluteExpiration = offset.Value;
			}
			else if (slidingExpiration.HasValue)
			{
				policy.SlidingExpiration = slidingExpiration.Value;
			}

			return policy;
		}
	}
}