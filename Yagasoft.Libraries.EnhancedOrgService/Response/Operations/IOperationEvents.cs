#region Imports

using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Response.Operations
{
	public interface IOperationEvents
	{
		event EventHandler<OperationStatusEventArgs> OperationStatusChanged;
		event EventHandler<OperationFailedEventArgs> OperationFailed;
	}
}
