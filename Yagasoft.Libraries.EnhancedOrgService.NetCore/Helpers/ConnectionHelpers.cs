#region Imports

using System;
using System.ServiceModel;
using System.Text.RegularExpressions;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.PowerPlatform.Dataverse.Client.Model;
using Microsoft.Xrm.Sdk;
using Yagasoft.Libraries.Common;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Helpers
{
	public static class ConnectionHelpers
	{
		private static readonly object lockObject = new();

		public static IOrganizationService CreateCrmService(string connectionString)
		{
			ServiceClient service;

			lock (lockObject)
			{
				service = new ServiceClient(connectionString);
			}

			var escapedString = Regex.Replace(connectionString, @"Password\s*?=.*?(?:;{0,1}$|;)",
				"Password=********;");

			var errorMessage = string.Empty;

			if (!service.IsReady)
			{
				if (service.LastException != null)
				{
					errorMessage += service.LastException.BuildExceptionMessage();
				}

				if (service.LastError.IsFilled())
				{
					errorMessage += $"\r\n\r\n{service.LastError}";
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

		public static bool? EnsureTokenValid(this IOrganizationService crmService, int tokenExpiryCheckSecs = 600)
		{
			if (crmService is not ServiceClient clientService)
			{
				return null;
			}

			if (!clientService.IsReady)
			{
				return false;
			}

			bool? isValidToken = null;

			// TODO are token expiry checks required in the new CrmSdk? Proxy is internal now!
			//var proxy = clientService.OrganizationServiceProxy;

			//if (proxy != null)
			//{
			//	// token is about to expire based on the configured threshold time from actual expiry
			//	var isTokenExpires = proxy.SecurityTokenResponse?.Response?.Lifetime?.Expires
			//		< DateTime.UtcNow.AddSeconds(tokenExpiryCheckSecs);

			//	if (!proxy.IsAuthenticated || isTokenExpires)
			//	{
			//		try
			//		{
			//			proxy.Authenticate();
			//		}
			//		catch
			//		{
			//			// ignored
			//		}
			//	}

			//	isValidToken = proxy.IsAuthenticated;
			//}
			//else
			//{
			//	var webProxy = clientService.OrganizationWebProxyClient;

			//	if (webProxy != null)
			//	{ }
			//}

			if (isValidToken == null)
			{
				return null;
			}

			return isValidToken.Value && clientService.IsReady;
		}
	}
}
