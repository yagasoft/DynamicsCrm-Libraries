#region Imports

using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Factories
{
	/// <summary>
	///     An enhanced service factory that returns the most common service types:
	///     <see cref="IEnhancedServiceFactory{TService}" />, <see cref="IEnhancedOrgService" />.
	/// </summary>
	public interface IDefaultEnhancedFactory : IEnhancedServiceFactory<IEnhancedOrgService>
	{ }
}
