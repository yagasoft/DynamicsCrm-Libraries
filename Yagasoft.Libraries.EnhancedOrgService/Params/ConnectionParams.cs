#region Imports

using System;
using Microsoft.Xrm.Sdk;
using Yagasoft.Libraries.Common;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Params
{
	public class ConnectionParams : ParamsBase
	{
		public string ConnectionString
		{
			get => connectionString;
			set
			{
				ValidateLock();
				value.RequireFilled(nameof(ConnectionString));
				connectionString = value;
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
				value.Require(nameof(DotNetDefaultConnectionLimit));
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
				value.Require(nameof(IsDotNetDisableWaitForConnectConfirm));
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
				value.Require(nameof(IsDotNetDisableNagleAlgorithm));
				isDotNetDisableNagleAlgorithm = value;
			}
		}

		/// <summary>
		///     A custom factory that will be used to create CRM connections instead of the library's built-in method.
		/// </summary>
		public Func<string, IOrganizationService> CustomIOrgSvcFactory
		{
			get => customIOrgSvcFactory;
			set
			{
				ValidateLock();
				value.Require(nameof(CustomIOrgSvcFactory));
				customIOrgSvcFactory = value;
			}
		}

		private string connectionString;
		private int? dotNetDefaultConnectionLimit;
		private bool? isDotNetDisableWaitForConnectConfirm;
		private bool? isDotNetDisableNagleAlgorithm;
		private Func<string, IOrganizationService> customIOrgSvcFactory;
	}
}
