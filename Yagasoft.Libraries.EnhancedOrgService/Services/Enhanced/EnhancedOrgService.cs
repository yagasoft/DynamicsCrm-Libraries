#region Imports

using Yagasoft.Libraries.EnhancedOrgService.Factories;
using Yagasoft.Libraries.EnhancedOrgService.Params;
using Yagasoft.Libraries.EnhancedOrgService.Pools;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced
{
	/// <inheritdoc cref="IEnhancedOrgService" />
	public class EnhancedOrgService : EnhancedOrgServiceBase
	{
		internal EnhancedOrgService(EnhancedServiceParams parameters) : base(parameters)
		{ }
	}
}
