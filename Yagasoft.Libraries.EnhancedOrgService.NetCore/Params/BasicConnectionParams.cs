#region Imports

using System;
using Microsoft.Xrm.Sdk;
using Yagasoft.Libraries.Common;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Params
{
	public class BasicConnectionParams : ParamsBase
	{
		public string? ConnectionString
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
		///     A custom factory that will be used to create CRM connections instead of the library's built-in method.
		/// </summary>
		public Func<string, IOrganizationService>? CustomIOrgSvcFactory
		{
			get => customIOrgSvcFactory;
			set
			{
				ValidateLock();
				customIOrgSvcFactory = value;
			}
		}

		private string? connectionString;
		private Func<string, IOrganizationService>? customIOrgSvcFactory;
	}
}
