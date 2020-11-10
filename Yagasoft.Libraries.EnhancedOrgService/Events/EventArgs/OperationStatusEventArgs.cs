using System;
using Yagasoft.Libraries.EnhancedOrgService.Response.Operations;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced;

namespace Yagasoft.Libraries.EnhancedOrgService.Events.EventArgs
{
	public class OperationStatusEventArgs
	{
		public Operation Operation { get; }
		public Status? Status { get; }

		public OperationStatusEventArgs(Operation operation, Status? status)
		{
			Operation = operation;
			Status = status;
		}
	}
}