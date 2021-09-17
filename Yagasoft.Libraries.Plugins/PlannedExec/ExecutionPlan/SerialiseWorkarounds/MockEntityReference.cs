#region Imports

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.SerialiseWorkarounds
{
	[DataContract]
	public class MockEntityReference
	{
		[DataMember]
		public Guid Id { get; set; }

		[DataMember]
		public string LogicalName { get; set; }

		[DataMember]
		public MockDictionary Keys { get; set; }
	}
}
