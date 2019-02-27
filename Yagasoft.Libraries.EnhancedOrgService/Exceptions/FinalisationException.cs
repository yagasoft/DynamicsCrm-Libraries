using System;
using System.Runtime.Serialization;

namespace Yagasoft.Libraries.EnhancedOrgService.Exceptions
{
	[Serializable]
	public class FinalisationException : Exception
	{
		public FinalisationException()
		{
		}

		public FinalisationException(string message) : base(message)
		{
		}

		public FinalisationException(string message, Exception innerException) : base(message, innerException)
		{
		}

		protected FinalisationException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}
