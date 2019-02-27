#region Imports

using System;
using Yagasoft.Libraries.EnhancedOrgService.Params;
using Yagasoft.Libraries.EnhancedOrgService.Response;
using Microsoft.Xrm.Sdk;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Services
{
	/// <summary>
	///     Author: Ahmed Elsawalhy<br />
	///     Version: 4.1.1
	/// </summary>
	public class EnhancedOrgService : EnhancedOrgServiceBase
	{
		public EnhancedOrgService(EnhancedServiceParams parameters) : base(parameters)
		{ }
	}
}
