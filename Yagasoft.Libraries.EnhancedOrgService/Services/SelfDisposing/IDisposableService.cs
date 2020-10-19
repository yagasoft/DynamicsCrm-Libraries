#region Imports

using System;
using Microsoft.Xrm.Sdk;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Services.SelfDisposing
{
	public interface IDisposableService : IOrganizationService, IDisposable
	{ }
}
