#region Imports

using System;
using System.Runtime.Serialization;
using Yagasoft.Libraries.EnhancedOrgService.Router;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Exceptions
{
	[Serializable]
	public class NodeSelectException : Exception
	{
		public NodeSelectException(string message) : base(message)
		{}

		public NodeSelectException(string message, Exception innerException) : base(message, innerException)
		{}
	}
}
