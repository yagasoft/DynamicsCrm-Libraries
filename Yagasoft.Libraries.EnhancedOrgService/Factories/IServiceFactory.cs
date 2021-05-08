#region Imports

using Microsoft.Xrm.Sdk;
using Yagasoft.Libraries.EnhancedOrgService.Pools;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Factories
{
	public interface IServiceFactory<out TService>
		where TService : IOrganizationService
	{
		/// <summary>
		///     Creates a Service using the factory parameters.
		/// </summary>
		TService CreateService();
	}
}
