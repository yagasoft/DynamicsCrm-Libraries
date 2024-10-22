#region Imports

using System.Runtime.Serialization;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.SerialiseWorkarounds
{
	[DataContract]
	public class MockKeyValue
	{
		[DataMember]
		public string Key { get; internal set; }

		[DataMember]
		public object Value { get; internal set; }

		public MockKeyValue(string key, object value)
		{
			Key = key;
			Value = value;
		}
	}
}
