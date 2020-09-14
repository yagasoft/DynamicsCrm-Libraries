#region Imports

using Microsoft.Xrm.Sdk;
using Yagasoft.Libraries.EnhancedOrgService.Pools;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Factories
{
	/// <summary>
	///     Handles creating Enhanced Org Services and their initialisation.
	///     No caching of connections is used; use <see cref="IEnhancedServicePool{TService}" /> instead.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	public interface IEnhancedServiceFactory<out TEnhancedOrgService>
		where TEnhancedOrgService : IEnhancedOrgService
	{
		/// <summary>
		///     Creates an Enhanced Org Service using the factory template.
		///     The threads are the actual CRM services used to communicate with CRM internally.
		///     This is useful for parallel requests when threading is used.
		///     Blocks if the threads are busy until a request has finished.
		/// </summary>
		/// <param name="threads">Number of internal CRM services to create.</param>
		TEnhancedOrgService CreateEnhancedService(int threads = 1);

		/// <summary>
		///     Creates a vanilla CRM service using the connection string from the factory template.
		/// </summary>
		IOrganizationService CreateCrmService();
	}
}
