#region Imports

using System;
using LinkDev.Libraries.EnhancedOrgService.Params;
using LinkDev.Libraries.EnhancedOrgService.Response;
using Microsoft.Xrm.Sdk;

#endregion

namespace LinkDev.Libraries.EnhancedOrgService.Services
{
	/// <summary>
	///     Author: Ahmed el-Sawalhy<br />
	///     Version: 4.1.1
	/// </summary>
	public class EnhancedOrgService : EnhancedOrgServiceBase
	{
		public EnhancedOrgService(EnhancedServiceParams parameters) : base(parameters)
		{ }
	}
}
