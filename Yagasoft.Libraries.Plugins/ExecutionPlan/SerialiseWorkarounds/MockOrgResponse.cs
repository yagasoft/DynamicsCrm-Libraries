using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.SerialiseWorkarounds
{
	[DataContract]
	public class MockOrgResponse
    {
	    [DataMember]
	    public string Name { get; set; }

	    [DataMember]
	    public MockDictionary Results { get; set; }
    }
}
