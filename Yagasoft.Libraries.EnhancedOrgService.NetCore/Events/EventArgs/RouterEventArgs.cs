#region Imports

using System;
using Yagasoft.Libraries.EnhancedOrgService.Router;
using Yagasoft.Libraries.EnhancedOrgService.Router.Node;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Events.EventArgs
{
	public enum Event
	{
		StatusChanged,
		NodeQueueAdded,
		NodeQueueRemoved,
		NodeStatusChanged,
		RoutingFailed
	}

	public class RouterEventArgs
	{
		public Event Event { get; }
		public INodeService Node { get; }
		public Status? RouterStatus { get; }
		public RouterMode? RouterMode { get; }
		public Exception LatestConnectionError { get; }

		public RouterEventArgs(Event @event, Status? routerStatus, RouterMode? routerMode, INodeService node = null,
			Exception latestConnectionError = null)
		{
			Event = @event;
			RouterStatus = routerStatus;
			RouterMode = routerMode;
			Node = node;
			LatestConnectionError = latestConnectionError;
		}
	}
}
