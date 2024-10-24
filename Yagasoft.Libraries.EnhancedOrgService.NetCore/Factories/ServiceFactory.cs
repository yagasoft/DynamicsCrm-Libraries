#region Imports

using System;
using System.Runtime.InteropServices;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.WebServiceClient;
using Yagasoft.Libraries.Common;
using Yagasoft.Libraries.EnhancedOrgService.Helpers;
using Yagasoft.Libraries.EnhancedOrgService.Params;
using AuthenticationType = Microsoft.PowerPlatform.Dataverse.Client.AuthenticationType;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Factories
{
	public class ServiceFactory : IServiceFactory<IOrganizationService>
	{
		private readonly ConnectionParams connectionParams;
		private readonly TimeSpan? tokenExpiryCheck;

		private readonly Func<string, IOrganizationService> customServiceFactory = ConnectionHelpers.CreateCrmService;

		private ServiceClient? serviceCloneBase;

		public ServiceFactory(ConnectionParams connectionParams, TimeSpan? tokenExpiryCheck)
		{
			connectionParams.Require(nameof(connectionParams));

			this.connectionParams = connectionParams;
			this.tokenExpiryCheck = tokenExpiryCheck;

			customServiceFactory = connectionParams.CustomIOrgSvcFactory ?? customServiceFactory;

			ParamHelpers.SetPerformanceParams(connectionParams);
		}

		public virtual IOrganizationService CreateService()
		{
			if (connectionParams == null)
			{
				throw new NotSupportedException("Creating a CRM connection is unsupported in this instance.");
			}

			IOrganizationService? service = null;

			var timeout = connectionParams.Timeout;

			if (timeout != null)
			{
				ServiceClient.MaxConnectionTimeout = timeout.Value;
			}

			if (serviceCloneBase == null)
			{
				service = customServiceFactory(connectionParams.ConnectionString ?? string.Empty);
				var serviceClient = service as ServiceClient;

				if (serviceClient is not null && connectionParams.IsMaxPerformance)
				{
					serviceClient.EnableAffinityCookie = true;
				}

				if (serviceClient?.ActiveAuthenticationType == AuthenticationType.OAuth)
				{
					serviceCloneBase = service as ServiceClient;
				}
			}

			if (serviceCloneBase != null)
			{
				service = serviceCloneBase.Clone();
			}

			service ??= customServiceFactory(connectionParams.ConnectionString ?? string.Empty);

			if (timeout == null)
			{
				return service;
			}
			
			if (service is OrganizationServiceProxy proxyService)
			{
				proxyService.Timeout = timeout.Value;
			}

			if (service is OrganizationWebProxyClient webProxyService)
			{
				webProxyService.InnerChannel.OperationTimeout = timeout.Value;
			}

			return service;
		}
	}
}
