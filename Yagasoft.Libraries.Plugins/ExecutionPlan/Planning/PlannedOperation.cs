using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.Base;
using Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.SdkMocks;
using Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.SerialiseWorkarounds;

namespace Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.Planning
{
	[DataContract]
    public class PlannedOperation
    {
		[DataMember]
	    public MockOrgRequest Request { get; set; }

		[DataMember]
	    public PlannedResponse Response { get; set; }
    }
}
