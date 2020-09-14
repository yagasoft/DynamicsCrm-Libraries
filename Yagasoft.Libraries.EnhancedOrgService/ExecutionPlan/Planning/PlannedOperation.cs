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
	internal class PlannedOperation
    {
		[DataMember]
	    public object Request { get; internal set; }

		[DataMember]
	    public PlannedResponse Response { get; internal set; }
    }
}
