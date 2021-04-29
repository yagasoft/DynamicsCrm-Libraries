#region Imports

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
		///     Threshold time in seconds from actual expiry of the connection token.
		///     Can be used in service pools to automatically renew tokens.<br />
		///     Default value: 2 minutes.
		/// </summary>
		public int? DequeueTimeoutInMillis
		{
			get => dequeueTimeoutInMillis ?? 2 * 60 * 1000;
			set
			{
				ValidateLock();
				value?.RequireAtLeast(1, nameof(DequeueTimeoutInMillis));
				dequeueTimeoutInMillis = value;
			}
		}

	    /// <summary>
	    ///     When a <see cref="IEnhancedServicePool{TService}.GetService" /> is passed a number of threads,
	    ///     an internally managed pool is created this service only.
	    ///     Enable this option to pre-create all connections (same as thread count);
	    ///     otherwise, connections are created when needed only.<br />
	    ///     Default value: false.
	    /// </summary>
	    public bool? IsEnableSelfPoolingWarmUp
	    {
	        get => isSelfPoolingWarmUp;
	        set
	        {
	            ValidateLock();
	            isSelfPoolingWarmUp = value;
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
		private int? dequeueTimeoutInMillis;
		private bool? isSelfPoolingWarmUp;
		private int? dotNetSetMinAppReservedThreads;
	}
}
