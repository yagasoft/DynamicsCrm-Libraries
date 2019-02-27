using System;
using System.Runtime.Serialization;

namespace Yagasoft.Libraries.EnhancedOrgService.Exceptions
{
	[Serializable]
	public class InitialisationException : Exception
	{
		public InitialisationException()
		{
		}

		public InitialisationException(string message) : base(message)
		{
		}

		public InitialisationException(string message, Exception innerException) : base(message, innerException)
		{
		}

		protected InitialisationException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}
