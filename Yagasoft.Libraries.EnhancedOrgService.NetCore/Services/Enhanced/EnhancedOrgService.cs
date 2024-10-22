#region Imports

using Yagasoft.Libraries.EnhancedOrgService.Factories;
using Yagasoft.Libraries.EnhancedOrgService.Params;
using Yagasoft.Libraries.EnhancedOrgService.Pools;
using Yagasoft.Libraries.EnhancedOrgService.Pools.WarmUp;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced
{
	/// <inheritdoc cref="IEnhancedOrgService" />
	public class EnhancedOrgService : EnhancedOrgServiceBase
	{
		protected internal EnhancedOrgService(ServiceParams parameters) : base(parameters)
		{ }
	}
}
