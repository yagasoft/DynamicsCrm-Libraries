#region Imports

using System;
using Microsoft.Xrm.Sdk;
using Yagasoft.Libraries.Common;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Params
{
	public class ConnectionParams : BasicConnectionParams
	{
		/// <summary>
		///     Max number of requests over a period of time. Default: 6000 requests over 5 minutes window.
		/// </summary>
		public ApiLimitParams? NumberOfRequests
		{
			get => numberOfRequests ??=
				new ApiLimitParams
				{
					Name = "Number of Requests",
					ErrorCode = -2147015902,
					ErrorMessage = $"Number of requests exceeded the limit of 6000 over time window of 300 seconds.",
					Limit = 300,
					Window = TimeSpan.FromSeconds(300)
				};
			set
			{
				ValidateLock();
				numberOfRequests = value;
			}
		}
		
		/// <summary>
		///     Max combined duration of execution of all requests over a period of time. Default: 20 minutes over 5 minutes window.
		/// </summary>
		public ApiLimitParams? ExecutionTime
		{
			get => executionTime ??=
				new ApiLimitParams
				{
					Name = "Execution Time",
					ErrorCode = -2147015903,
					ErrorMessage =
						$"Combined execution time of incoming requests exceeded limit of 1,200,000 milliseconds over time window of 300 seconds. Decrease number of concurrent requests or reduce the duration of requests and try again later.",
					Limit = 1200000,
					Window = TimeSpan.FromSeconds(300)
				};
			set
			{
				ValidateLock();
				executionTime = value;
			}
		}

		
		/// <summary>
		///     Max number of requests in parallel. Default: 52.
		/// </summary>
		public ApiLimitParams? ConcurrentRequests
		{
			get => concurrentRequests ??=
				new ApiLimitParams
				{
					Name = "Concurrent Requests",
					ErrorCode = -2147015898,
					ErrorMessage = $"Number of concurrent requests exceeded the limit of 52.",
					Limit = 52
				};
			set
			{
				ValidateLock();
				concurrentRequests = value;
			}
		}

		/// <summary>
		///     Change the connection timeout when making requests to CRM. Default: 2 minutes.<br />
		///     This is an app-wide (global on the .Net Framework level) setting.
		/// </summary>
		public TimeSpan? Timeout
		{
			get => timeout;
			set
			{
				ValidateLock();
				value?.TotalSeconds.RequireAtLeast(1, nameof(Timeout));
				timeout = value;
			}
		}

		/// <summary>
		///     Change max connections from .NET to a remote service. Default: 2.<br />
		///     This is an app-wide (global on the .Net Framework level) setting.
		/// </summary>
		public int? DotNetDefaultConnectionLimit
		{
			get => dotNetDefaultConnectionLimit;
			set
			{
				ValidateLock();
				value?.RequireAtLeast(1, nameof(DotNetDefaultConnectionLimit));
				dotNetDefaultConnectionLimit = value;
			}
		}

		/// <summary>
		///     Turn off the 'Expect 100' to continue message. If 'true', will cause the caller to <b>NOT</b> wait until the
		///     round-trip confirms a connection to the server. Default: false.<br />
		///     This is an app-wide (global on the .Net Framework level) setting.
		/// </summary>
		public bool? IsDotNetDisableWaitForConnectConfirm
		{
			get => isDotNetDisableWaitForConnectConfirm;
			set
			{
				ValidateLock();
				isDotNetDisableWaitForConnectConfirm = value;
			}
		}

		/// <summary>
		///     If 'true', will make communication packets go out to the network faster, but will increase network chatter.
		///     Default: false.<br />
		///     This is an app-wide (global on the .Net Framework level) setting.
		/// </summary>
		public bool? IsDotNetDisableNagleAlgorithm
		{
			get => isDotNetDisableNagleAlgorithm;
			set
			{
				ValidateLock();
				isDotNetDisableNagleAlgorithm = value;
			}
		}

		internal bool IsMaxPerformance;
		
		private TimeSpan? timeout;
		private int? dotNetDefaultConnectionLimit;
		private bool? isDotNetDisableWaitForConnectConfirm;
		private bool? isDotNetDisableNagleAlgorithm;

		private ApiLimitParams? numberOfRequests;
		private ApiLimitParams? executionTime;
		private ApiLimitParams? concurrentRequests;

		public bool IsApiLimit(int errorCode) =>
			GetType().GetProperties()
				.Where(p => p.PropertyType.IsAssignableTo(typeof(ApiLimitParams)))
				.Select(p => p.GetValue(this)).OfType<ApiLimitParams>()
				.Any(property => property.ErrorCode == errorCode);
	}
}
