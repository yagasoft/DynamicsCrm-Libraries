using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Yagasoft.Libraries.Common;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Tooling.Connector;

namespace Yagasoft.Libraries.EnhancedOrgService.Helpers
{
    public class ConnectionHelpers
    {
	    private static string latestStringUsed = "";
		private static readonly object lockObject = new object();

	    public static IOrganizationService CreateCrmService(string connectionString)
	    {
		    CrmServiceClient service;

		    lock (lockObject)
		    {
			    if (latestStringUsed != connectionString
				    && !connectionString.ToLower().Contains("requirenewinstance"))
			    {
				    latestStringUsed = connectionString;
				    connectionString = connectionString.Trim(';', ' ');
				    connectionString += ";RequireNewInstance=true";
			    }

			    service = new CrmServiceClient(connectionString);
		    }

		    var escapedString = Regex.Replace(connectionString, @"Password\s*?=.*?(?:;{0,1}$|;)",
			    "Password=********;");

		    try
		    {
			    if (!string.IsNullOrEmpty(service.LastCrmError) || service.LastCrmException != null)
			    {
				    throw new ServiceActivationException(
					    $"Can't create connection to: \"{escapedString}\" due to \"{service.LastCrmError}\"");
			    }

			    return service;
		    }
		    catch (Exception ex)
		    {
			    var errorMessage = service.LastCrmError
				    ?? (service.LastCrmException != null ? CrmHelpers.BuildExceptionMessage(service.LastCrmException) : null)
					    ?? CrmHelpers.BuildExceptionMessage(ex);
			    throw new ServiceActivationException($"Can't create connection to: \"{escapedString}\" due to\r\n{errorMessage}");
		    }
	    }

        public static bool? EnsureTokenValid(IOrganizationService crmService, int tokenExpiryCheckSecs = 600)
        {
            if (crmService != null && crmService is CrmServiceClient clientService)
            {
                if (clientService.LastCrmError.IsNotEmpty() || clientService.LastCrmException != null)
                {
                    return false;
                }

                var proxy = clientService.OrganizationServiceProxy;

                if (proxy == null)
                {
                    return null;
                }

                if (!proxy.IsAuthenticated
                    || proxy.SecurityTokenResponse?.Response?.Lifetime?.Expires
                        < DateTime.UtcNow.AddSeconds(tokenExpiryCheckSecs))
                {
                    proxy.Authenticate();
                }

                return proxy.IsAuthenticated;
            }

            return null;
        }
    }
}
