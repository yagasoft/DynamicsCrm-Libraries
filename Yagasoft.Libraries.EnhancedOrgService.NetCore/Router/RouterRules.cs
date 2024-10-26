#region Imports

using System;
using Microsoft.Crm.Sdk.Messages;
using Yagasoft.Libraries.EnhancedOrgService.Params;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Router
{
	public enum RouterMode
	{
		/// <summary>
		///     Use each node equally in order, and then wrap around the list at the end.
		/// </summary>
		RoundRobin,

		/// <summary>
		///     Same as round robin, but use higher rated nodes a bit more.
		/// </summary>
		WeightedRoundRobin,

		/// <summary>
		///     Use a single node, but fallback on retry failure.
		/// </summary>
		StaticWithFallback,

		/// <summary>
		///     Use the least loaded node -- node with least pending requests.
		/// </summary>
		LeastLoaded,

		/// <summary>
		///     Sort nodes by their latency and use the fastest one.
		/// </summary>
		LeastLatency
	}

	public class RouterRules : ParamsBase
	{
		public RouterMode? RouterMode
		{
			get => routerMode;
			set
			{
				ValidateLock();

				if (value == Router.RouterMode.StaticWithFallback)
				{
					IsFallbackEnabled = true;
				}

				routerMode = value;
			}
		}

		public bool? IsFallbackEnabled
		{
			get => isFallbackEnabled;
			set
			{
				ValidateLock();

				if (RouterMode == Router.RouterMode.StaticWithFallback && value != true)
				{
					throw new NotSupportedException("Cannot disable fallback if router mode requires fallback.");
				}

				isFallbackEnabled = value;
			}
		}

		/// <summary>
		///     The time between <see cref="WhoAmIRequest" />s to check for node latency.<br />
		///     Only used with 'least latency' algorithm, currently.<br />
		///     Default: 10 seconds.
		/// </summary>
		public TimeSpan? NodeCheckInterval
		{
			get => nodeCheckInterval;
			set
			{
				ValidateLock();
				nodeCheckInterval = value;
			}
		}

		private RouterMode? routerMode;
		private bool? isFallbackEnabled;
		private TimeSpan? nodeCheckInterval;
	}
}
