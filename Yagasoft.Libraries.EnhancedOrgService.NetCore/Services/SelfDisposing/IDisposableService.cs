#region Imports

using System;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Services.SelfDisposing
{
	public interface IDisposableService : IOrganizationServiceAsync2, IDisposable
	{ }
}
