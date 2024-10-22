#region Imports

using System;
using Yagasoft.Libraries.EnhancedOrgService.Router.Node;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Exceptions
{
	[Serializable]
	public class NodeInitException : Exception
	{
		public INodeService NodeService { get; protected internal set; }

		public NodeInitException(string message, INodeService nodeService) : base(message)
		{
			NodeService = nodeService;
		}

		public NodeInitException(string message, Exception innerException, INodeService nodeService) : base(message, innerException)
		{
			NodeService = nodeService;
		}
	}
}
