#region Imports

using System;
using Yagasoft.Libraries.Common;
using Yagasoft.Libraries.EnhancedOrgService.Pools;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Params
{
	public class PoolParams : ParamsBase
	{
		/// <summary>
		///     Default value: false.
		/// </summary>
		public bool IsAutoPoolSize
		{
			get => isAutoPoolSize;
			set
			{
				ValidateLock();
				isAutoPoolSize = value;
			}
		}

		/// <summary>
		///     Default value: <see cref="int.MaxValue"/> connections, which sets <see cref="IsAutoPoolSize"/> to true.
		/// </summary>
		public int? PoolSize
		{
			get => poolSize ?? int.MaxValue;
			set
			{
				ValidateLock();

				if (value < 1)
				{
					IsAutoPoolSize = true;
					poolSize = null;
					return;
				}
				
				poolSize = value;
			}
		}

		/// <summary>
		///     Threshold time from actual expiry of the connection token.
		///     Can be used in service pools to automatically renew tokens.<br />
		///     Default value: 1 hour.
		/// </summary>
		public TimeSpan? TokenExpiryCheck
		{
			get => tokenExpiryCheck ?? TimeSpan.FromHours(1);
			set
			{
				ValidateLock();
			    value?.TotalSeconds.RequireAtLeast(1, nameof(TokenExpiryCheck));
				tokenExpiryCheck = value;
			}
		}

		/// <summary>
		///     Threshold time to wait for a connection to become available in the connection pool.<br />
		///     Default value: 2 minutes.
		/// </summary>
		public TimeSpan? DequeueTimeout
		{
		    get => dequeueTimeout ?? TimeSpan.FromMinutes(2);
		    set
		    {
		        ValidateLock();
		        value?.TotalSeconds.RequireAtLeast(1, nameof(DequeueTimeout));
		        dequeueTimeout = value;
		    }
		}

	    /// <summary>
		///     Bump up the min threads reserved for this app. Default: 4.<br />
		///     This is an app-wide (global on the .Net Framework level) setting.
		/// </summary>
		public int? DotNetSetMinAppReservedThreads
		{
			get => dotNetSetMinAppReservedThreads;
			set
			{
				ValidateLock();
				value?.RequireAtLeast(1, nameof(DotNetSetMinAppReservedThreads));
				dotNetSetMinAppReservedThreads = value;
			}
		}

		internal bool IsMaxPerformance;
		
		private bool isAutoPoolSize;
		private int? poolSize;
		private TimeSpan? tokenExpiryCheck;
		private TimeSpan? dequeueTimeout;
		private int? dotNetSetMinAppReservedThreads;
	}
}
