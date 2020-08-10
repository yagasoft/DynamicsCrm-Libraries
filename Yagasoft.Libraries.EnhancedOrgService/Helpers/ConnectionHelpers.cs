#region Imports

using System;
using System.ServiceModel;
using System.Text.RegularExpressions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Tooling.Connector;
using Yagasoft.Libraries.Common;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Helpers
{
	public class ConnectionHelpers
	{
		private static readonly object lockObject = new object();

		public static IOrganizationService CreateCrmService(string connectionString)
		{
			CrmServiceClient service;

			lock (lockObject)
			{
				service = new CrmServiceClient(connectionString);
			}

			var escapedString = Regex.Replace(connectionString, @"Password\s*?=.*?(?:;{0,1}$|;)",
				"Password=********;");

			var errorMessage = string.Empty;

			if (!service.IsReady)
			{
				if (service.LastCrmException != null)
				{
					errorMessage += CrmHelpers.BuildExceptionMessage(service.LastCrmException);
				}

				if (service.LastCrmError.IsFilled())
				{
					errorMessage += $"\r\n\r\n{service.LastCrmError}";
				}

				if (errorMessage.IsEmpty())
				{
					errorMessage += "CRM service did not report a specific reason.";
				}
			}

			if (errorMessage.IsFilled())
			{
				throw new ServiceActivationException($"Can't create connection to: \"{escapedString}\" due to\r\n{errorMessage}");
			}

			return service;
		}

		public static bool? EnsureTokenValid(IOrganizationService crmService, int tokenExpiryCheckSecs = 600)
		{
			if (crmService == null || !(crmService is CrmServiceClient clientService))
			{
				return null;
			}

			if (!clientService.IsReady)
			{
				return false;
			}

			var proxy = clientService.OrganizationServiceProxy;

			if (proxy == null)
			{
				return null;
			}

			// token is about to expire based on the configured threshold time from actual expiry
			var isTokenExpires = proxy.SecurityTokenResponse?.Response?.Lifetime?.Expires
				< DateTime.UtcNow.AddSeconds(tokenExpiryCheckSecs);

			if (!proxy.IsAuthenticated || isTokenExpires)
			{
				proxy.Authenticate();
			}

			return proxy.IsAuthenticated && clientService.IsReady;
		}
	}
}
