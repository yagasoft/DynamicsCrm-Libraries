using System;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced;

namespace Yagasoft.Libraries.EnhancedOrgService.Response.Operations
{
	public class OperationStatusEventArgs
	{
		public IEnhancedOrgService Service { get; }
		public Operation Operation { get; }

		public OperationStatusEventArgs(IEnhancedOrgService service, Operation operation)
		{
			Service = service;
			Operation = operation;
		}
	}
}