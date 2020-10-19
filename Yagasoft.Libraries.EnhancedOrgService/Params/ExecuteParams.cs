using Yagasoft.Libraries.EnhancedOrgService.Router;

namespace Yagasoft.Libraries.EnhancedOrgService.Params
{
	/// <summary>
	///     Operation-specific parameters.<br />
	///     Some features must be enabled on the service itself in order to take effect; e.g. caching, transaction, ... etc.
	///     <br />
	///     Auto-retry can be enabled per operation even if disabled on the service itself.
	/// </summary>
	public class ExecuteParams : EnhancedServiceParamsBase
	{
		public AutoRetryParams AutoRetryParams { get; set; }
		public bool IsExcludeFromHistory { get; set; }
		public bool IsNotDeferred { get; set; }
	}
}
