#region Imports

using System.Runtime.Serialization;
using Microsoft.Xrm.Sdk;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.SerialiseWorkarounds
{
	[DataContract]
	internal class MockEntityCollection
	{
		[DataMember]
		public MockEntity[] Collection { get; internal set; }
	}

	[DataContract]
	internal class MockEntityReferenceCollection
	{
		[DataMember]
		public EntityReference[] Collection { get; internal set; }
	}

	[DataContract]
	internal class MockOptionSetValueCollection
	{
		[DataMember]
		public OptionSetValue[] Collection { get; internal set; }
	}
}
