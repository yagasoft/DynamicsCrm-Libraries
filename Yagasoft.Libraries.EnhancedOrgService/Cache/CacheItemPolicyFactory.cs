#region Imports

using System;
using System.Runtime.Caching;
using Microsoft.Xrm.Client.Services;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Cache
{
	internal class CacheItemPolicyFactory : ICacheItemPolicyFactory
	{
		private readonly double? absoluteExpirySecs;
		private readonly TimeSpan? slidingExpiration;
		private readonly CacheItemPriority? priority;

		public CacheItemPolicyFactory(double absoluteExpirySecs, CacheItemPriority? priority)
		{
			this.absoluteExpirySecs = absoluteExpirySecs;
			this.priority = priority;
		}

		public CacheItemPolicyFactory(TimeSpan slidingExpiration, CacheItemPriority? priority)
		{
			this.slidingExpiration = slidingExpiration;
			this.priority = priority;
		}

		public CacheItemPolicy Create()
		{
			var policy = new CacheItemPolicy();

			if (absoluteExpirySecs.HasValue)
			{
				policy.AbsoluteExpiration = DateTime.Now.AddSeconds(absoluteExpirySecs.Value);
			}
			else if (slidingExpiration.HasValue)
			{
				policy.SlidingExpiration = slidingExpiration.Value;
			}

			if (priority.HasValue)
			{
				policy.Priority = priority.Value;
			}

			return policy;
		}
	}
}
