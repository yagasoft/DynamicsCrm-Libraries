#region Imports

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.Xrm.Sdk.Query;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.SerialiseWorkarounds
{
	[DataContract]
	internal class MockColumnSet
	{
		[DataMember]
		public bool AllColumns { get; internal set; }

		[DataMember]
		public string[] Columns { get; internal set; }
	}
}
