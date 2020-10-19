using System;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced;

namespace Yagasoft.Libraries.EnhancedOrgService.Response.Operations
{
	public class OperationFailedEventArgs
	{
		public IEnhancedOrgService Service { get; }
		public Operation Operation { get; }
		public int LastRetryCount { get; }
		public TimeSpan LastRetryInterval { get; }

		public OperationFailedEventArgs(IEnhancedOrgService service, Operation operation, int lastRetryCount, TimeSpan lastRetryInterval)
		{
			Service = service;
			Operation = operation;
			LastRetryCount = lastRetryCount;
			LastRetryInterval = lastRetryInterval;
		}
	}
}