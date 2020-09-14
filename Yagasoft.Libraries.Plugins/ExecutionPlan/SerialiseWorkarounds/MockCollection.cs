#region Imports

using System.Runtime.Serialization;
using Microsoft.Xrm.Sdk;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.SerialiseWorkarounds
{
	[DataContract]
	public class MockEntityCollection
	{
		[DataMember]
		public MockEntity[] Collection { get; set; }
	}

	[DataContract]
	public class MockEntityReferenceCollection
	{
		[DataMember]
		public EntityReference[] Collection { get; set; }
	}

	[DataContract]
	public class MockOptionSetValueCollection
	{
		[DataMember]
		public OptionSetValue[] Collection { get; set; }
	}
}
