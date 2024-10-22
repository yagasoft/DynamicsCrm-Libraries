#region Imports

using System;
using Yagasoft.Libraries.EnhancedOrgService.Router.Node;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Events.EventArgs
{
	public class NodeStatusEventArgs
	{
		public INodeService Node { get; }
		public NodeStatus? Status { get; }
		public Exception LatestConnectionError { get; }

		public NodeStatusEventArgs(INodeService node, NodeStatus? status, Exception latestConnectionError = null)
		{
			Node = node;
			Status = status;
			LatestConnectionError = latestConnectionError;
		}
	}
}
