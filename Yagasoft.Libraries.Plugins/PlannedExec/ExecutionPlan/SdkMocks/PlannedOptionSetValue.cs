#region Imports

using System;
using System.Runtime.Serialization;
using Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.Base;
using Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.Planning;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.SdkMocks
{
	[DataContract]
	public class PlannedOptionSetValue : PlannedMapBase
	{
		[DataMember]
		public PlannedValue Value { get; set; }

		public PlannedOptionSetValue(Guid? parentId, string alias) : base(parentId, alias)
		{
			InnerDictionary["Value"] = Value = new PlannedValue(Id, "Value");
		}
	}
}
