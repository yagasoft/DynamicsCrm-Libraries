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
		private static readonly SemaphoreSlim createSemaphore = new(20);

		public static async Task<IOrganizationService> CreateCrmService(string connectionString)
		{
			ServiceClient service = null;
			
			var errorMessage = string.Empty;
			Exception? exception = null;
			
			try
			{
				await createSemaphore.WaitAsync();
				
				service = await Task.Factory.StartNew(() => new ServiceClient(connectionString), TaskCreationOptions.LongRunning);
				// warm up
				await service.ExecuteAsync(new WhoAmIRequest());
			}
			catch (Exception ex)
			{
				exception = ex;
				errorMessage = ex.Message;
			}
			finally
			{
				createSemaphore.Release();
			}

			if (service is not null && exception is null)
			{
				try
				{
					// warm up
					await service.ExecuteAsync(new WhoAmIRequest());
				}
				catch (Exception ex)
				{
					exception = ex;
					errorMessage = ex.Message;
				}
			}
			
			var escapedString = Regex.Replace(connectionString, @"Password\s*?=.*?(?:;{0,1}$|;)",
				"Password=********;");

			if (service?.IsReady is false)
			{
				if (service.LastException != null)
				{
					errorMessage += service.LastException.BuildExceptionMessage();
					exception = service.LastException;
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
				throw new ServiceActivationException($"Can't create connection to: \"{escapedString}\" due to\r\n{errorMessage}", exception);
			}

			return service;
		}
	}
}
