#region Imports

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.SerialiseWorkarounds
{
	[DataContract]
	internal class MockEntityReference
	{
		[DataMember]
		public Guid Id { get; internal set; }

		[DataMember]
		public string LogicalName { get; internal set; }

		[DataMember]
		public MockDictionary Keys { get; internal set; }
	}
}
