#region Imports

using System;
using System.Xml.Linq;

using Microsoft.Xrm.Sdk;
using Yagasoft.Libraries.Common;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Params
{
	public class ApiLimitParams : ParamsBase
	{
		/// <summary>
		///     Name of the API limit.
		/// </summary>
		public string? Name
		{
			get => name;
			set
			{
				ValidateLock();
				name = value;
			}
		}

		/// <summary>
		///     The error code related to this API limit.
		/// </summary>
		public int? ErrorCode
		{
			get => errorCode;
			set
			{
				ValidateLock();
				errorCode = value;
			}
		}

		/// <summary>
		///     The error message related to the error code.
		/// </summary>
		public string? ErrorMessage
		{
			get => errorMessage;
			set
			{
				ValidateLock();
				errorMessage = value;
			}
		}

		/// <summary>
		///     The limit value.
		/// </summary>
		public int? Limit
		{
			get => limit;
			set
			{
				ValidateLock();
				limit = value;
			}
		}

		/// <summary>
		///     The time window for the limit.
		/// </summary>
		public TimeSpan? Window
		{
			get => window;
			set
			{
				ValidateLock();
				window = value;
			}
		}

		private string? name;
		private string? errorMessage;
		private int? errorCode;
		private int? limit;
		private TimeSpan? window;
	}
}
