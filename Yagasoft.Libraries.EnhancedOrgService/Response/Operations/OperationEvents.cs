#region Imports

using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Response.Operations
{
	public class OperationEvents : IOperationEvents
	{
		public virtual event EventHandler<OperationStatusEventArgs> OperationStatusChanged;
		public virtual event EventHandler<OperationFailedEventArgs> OperationFailed;
	}
}
