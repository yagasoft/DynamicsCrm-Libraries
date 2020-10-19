#region Imports

using Microsoft.Xrm.Sdk;
using Yagasoft.Libraries.EnhancedOrgService.Pools;
using Yagasoft.Libraries.EnhancedOrgService.Response.Operations;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced.Transactions;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Factories
{
	public interface IServiceFactory
	{
		/// <summary>
		///     Creates a vanilla CRM service using the connection string from the factory template.
		/// </summary>
		IOrganizationService CreateCrmService();
	}
}
