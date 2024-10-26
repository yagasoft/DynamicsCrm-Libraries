#region Imports

using Microsoft.Xrm.Sdk;

using Yagasoft.Libraries.EnhancedOrgService.Params;
using Yagasoft.Libraries.EnhancedOrgService.Pools;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Factories
{
	public interface IServiceFactory<TService>
		where TService : IOrganizationService
	{
		ServiceParams ServiceParams { get; }

		/// <summary>
		///     Creates a Service using the factory parameters.
		/// </summary>
		Task<TService> CreateService();
	}
}
