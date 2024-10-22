#region Imports

using Yagasoft.Libraries.EnhancedOrgService.Params;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Factories
{
	/// <summary>
	///     An enhanced service factory that returns the most common service types:
	///     <see cref="EnhancedServiceFactory{TService,TEnhancedOrgService}" />, <see cref="IEnhancedOrgService" />,
	///     <see cref="Services.Enhanced.EnhancedOrgService" />.
	/// </summary>
	public class DefaultEnhancedFactory
		: EnhancedServiceFactory<IEnhancedOrgService, Services.Enhanced.EnhancedOrgService>, IDefaultEnhancedFactory
	{
		public DefaultEnhancedFactory(string connectionString) : base(new ServiceParams(connectionString))
		{ }

		public DefaultEnhancedFactory(ServiceParams parameters) : base(parameters)
		{ }
	}
}
