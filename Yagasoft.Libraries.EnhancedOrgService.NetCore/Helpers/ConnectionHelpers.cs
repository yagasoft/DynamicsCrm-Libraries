#region Imports

using System;
using System.ServiceModel;
using System.Text.RegularExpressions;
using Microsoft.Crm.Sdk.Messages;
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

			// warm up
			try
			{
				service.Execute(new WhoAmIRequest());
			}
			catch
			{ }
			
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
	}
}
