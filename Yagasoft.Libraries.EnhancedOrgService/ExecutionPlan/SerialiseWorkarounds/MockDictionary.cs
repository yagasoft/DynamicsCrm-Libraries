#region Imports

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.SerialiseWorkarounds
{
	[DataContract]
	public class MockDictionary : IEnumerable<MockKeyValue>
	{
		[DataMember]
		public List<MockKeyValue> InnerList { get; set; }

		public MockDictionary(IEnumerable<MockKeyValue> list = null)
		{
			InnerList = list == null ? new List<MockKeyValue>() : new List<MockKeyValue>(list);
		}

		public object this[string key]
		{
			get => InnerList.FirstOrDefault(e => e.Key == key)?.Value;
			internal set
			{
				InnerList.RemoveAll(e => e.Key == key);
				InnerList.Add(new MockKeyValue(key, value));
			}
		}

		public IEnumerator<MockKeyValue> GetEnumerator()
		{
			return InnerList.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return InnerList.GetEnumerator();
		}
	}
}
