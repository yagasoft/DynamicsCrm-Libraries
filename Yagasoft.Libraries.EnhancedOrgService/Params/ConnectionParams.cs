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
		///     Change max connections from .NET to a remote service, default: 2.<br />
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
		///     Turn off the Expect 100 to continue message. If 'true', will cause the caller to wait until the round-trip confirms a
		///     connection to the server.<br />
		///     This is an app-wide (global on the .Net Framework level) setting.
		/// </summary>
		public bool? IsDotNetWaitForConnectConfirm
		{
			get => isDotNetWaitForConnectConfirm;
			set
			{
				ValidateLock();
				value.Require(nameof(IsDotNetWaitForConnectConfirm));
				isDotNetWaitForConnectConfirm = value;
			}
		}

		/// <summary>
		///     If 'true', can decrease overall transmission overhead but can cause delay in data packet arrival.<br />
		///     This is an app-wide (global on the .Net Framework level) setting.
		/// </summary>
		public bool? IsDotNetUseNagleAlgorithm
		{
			get => isDotNetUseNagleAlgorithm;
			set
			{
				ValidateLock();
				value.Require(nameof(IsDotNetUseNagleAlgorithm));
				isDotNetUseNagleAlgorithm = value;
			}
		}

		/// <summary>
		///     A custom factory that will be used to create CRM connections instead of the library built-in method.
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
		private bool? isDotNetWaitForConnectConfirm;
		private bool? isDotNetUseNagleAlgorithm;
		private Func<string, IOrganizationService> customIOrgSvcFactory;
	}
}
