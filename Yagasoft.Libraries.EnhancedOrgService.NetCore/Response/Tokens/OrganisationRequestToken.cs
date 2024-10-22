#region Imports

using Microsoft.Xrm.Sdk;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Response.Tokens
{
	public class OrganisationRequestToken<TValue> : Token<TValue>
	{
		public OrganizationRequest Request { get; internal set; }
	}
}
