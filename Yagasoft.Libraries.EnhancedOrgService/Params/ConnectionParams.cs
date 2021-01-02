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

		private TimeSpan? timeout;
		private int? dotNetDefaultConnectionLimit;
		private bool? isDotNetDisableWaitForConnectConfirm;
		private bool? isDotNetDisableNagleAlgorithm;
	}
}
