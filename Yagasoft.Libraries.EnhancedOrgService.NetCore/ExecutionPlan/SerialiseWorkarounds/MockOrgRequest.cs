using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.SerialiseWorkarounds
{
	[DataContract]
	public class MockOrgRequest
    {
	    [DataMember]
	    public string Name { get; internal set; }

	    [DataMember]
	    public MockDictionary Parameters { get; internal set; }
    }
}
