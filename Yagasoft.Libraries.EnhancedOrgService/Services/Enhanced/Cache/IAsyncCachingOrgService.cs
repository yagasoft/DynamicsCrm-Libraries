#region Imports



#endregion

using System;
using Microsoft.Xrm.Sdk;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced.Async;

namespace Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced.Cache
{
	public interface IAsyncCachingOrgService : ICachingOrgService, IAsyncOrgService
	{ }
}
