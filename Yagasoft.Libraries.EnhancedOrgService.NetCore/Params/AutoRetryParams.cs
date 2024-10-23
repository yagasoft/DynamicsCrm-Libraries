#region Imports

using System;
using System.Collections.Generic;

using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Yagasoft.Libraries.Common;
using Yagasoft.Libraries.EnhancedOrgService.Response.Operations;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Params
{
	public class AutoRetryParams : ParamsBase
	{
		/// <summary>
		///     Maximum number of times to retry an operation.
		/// </summary>
		public int? MaxRetryCount
		{
			get => maxRetryCount;
			set
			{
				ValidateLock();
				value?.RequireAtLeast(0, nameof(MaxRetryCount));
				maxRetryCount = value;
			}
		}

		/// <summary>
		///     The time to wait between retry attempts.
		/// </summary>
		public TimeSpan? RetryInterval
		{
			get => retryInterval;
			set
			{
				ValidateLock();
				value?.TotalMilliseconds.RequireAtLeast(1, nameof(RetryInterval));
				retryInterval = value;
			}
		}

		/// <summary>
		///     The time to add on each retry attempt<br />
		///     Next retry wait time = current retry wait time (starts with
		///     <see cref="RetryInterval" /> as base) * <see cref="BackoffMultiplier" />.
		/// </summary>
		public double? BackoffMultiplier
		{
			get => backoffMultiplier;
			set
			{
				ValidateLock();
				value?.RequireAtLeast(1, nameof(BackoffMultiplier));
				backoffMultiplier = value;
			}
		}

		/// <summary>
		///     Maxmimum time to wait between retries.
		///     This is useful for when the <see cref="BackoffMultiplier" /> causes the interval to be too long at one point.
		/// </summary>
		public TimeSpan? MaxmimumRetryInterval
		{
			get => maxmimumRetryInterval;
			set
			{
				ValidateLock();
				value?.TotalMilliseconds.RequireAtLeast(1, nameof(MaxmimumRetryInterval));
				maxmimumRetryInterval = value;
			}
		}

		/// <summary>
		///     Define functions to retry failures. If one works, then the operation will be considered a success at its original
		///     location.
		/// </summary>
		public readonly List<Func<Func<IOrganizationServiceAsync2, Task<object>>, Operation, ExecuteParams, Exception, Task<object>>>
			CustomRetryFunctions = new();

		private int? maxRetryCount;
		private TimeSpan? retryInterval;
		private double? backoffMultiplier;
		private TimeSpan? maxmimumRetryInterval;
	}
}
