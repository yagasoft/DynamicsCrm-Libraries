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
		///     Default value: 2 connections.
		/// </summary>
		public int? PoolSize
		{
			get => poolSize ?? 2;
			set
			{
				ValidateLock();
				value?.RequireAtLeast(1, nameof(PoolSize));
				poolSize = value;
			}
		}

		/// <summary>
		///     Threshold time in seconds from actual expiry of the connection token.
		///     Can be used in service pools to automatically renew tokens.<br />
		///     Default value: 1 hour.
		/// </summary>
		public int? TokenExpiryCheckSecs
		{
			get => tokenExpiryCheckSecs ?? 60 * 60;
			set
			{
				ValidateLock();
				value?.RequireAtLeast(1, nameof(TokenExpiryCheckSecs));
				tokenExpiryCheckSecs = value;
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
	    ///     When a pool is created
	    ///     or a <see cref="IEnhancedServicePool{TService}.GetService" /> is passed a number of threads,
	    ///     this option enables pre-creating maximum number of connections to be ready when needed;
	    ///     otherwise, connections are created when needed only.<br />
	    ///     Default value: false.
	    /// </summary>
	    public bool? IsAutoPoolWarmUp
	    {
	        get => isInnerPoolingWarmUp;
	        set
	        {
	            ValidateLock();
	            isInnerPoolingWarmUp = value;
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

		private int? poolSize;
		private int? tokenExpiryCheckSecs;
		private TimeSpan? dequeueTimeout;
		private bool? isInnerPoolingWarmUp;
		private int? dotNetSetMinAppReservedThreads;
	}
}
