#region Imports

using System.Runtime.Serialization;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.SerialiseWorkarounds
{
	[DataContract]
	public class MockKeyValue
	{
		[DataMember]
		public string Key { get; set; }

		[DataMember]
		public object Value { get; set; }

		public MockKeyValue(string key, object value)
		{
			Key = key;
			Value = value;
		}
	}
}
