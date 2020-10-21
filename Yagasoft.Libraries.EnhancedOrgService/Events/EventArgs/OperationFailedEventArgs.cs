using System;
using Yagasoft.Libraries.EnhancedOrgService.Response.Operations;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced;

namespace Yagasoft.Libraries.EnhancedOrgService.Events.EventArgs
{
	public class OperationFailedEventArgs
	{
		public Operation Operation { get; }
		public int LastRetryCount { get; }
		public TimeSpan LastRetryInterval { get; }

		public OperationFailedEventArgs(Operation operation, int lastRetryCount, TimeSpan lastRetryInterval)
		{
			Operation = operation;
			LastRetryCount = lastRetryCount;
			LastRetryInterval = lastRetryInterval;
		}
	}
}