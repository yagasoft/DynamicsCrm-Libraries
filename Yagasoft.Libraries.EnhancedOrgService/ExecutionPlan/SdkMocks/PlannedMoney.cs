#region Imports

using System;
using System.Runtime.Serialization;
using Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.Base;
using Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.Planning;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.SdkMocks
{
	[DataContract]
	internal class PlannedMoney : PlannedMapBase
	{
		[DataMember]
		public PlannedValue Value { get; internal set; }

		internal PlannedMoney(Guid? parentId, string alias) : base(parentId, alias)
		{
			InnerDictionary["Value"] = Value = new PlannedValue(Id, "Value");
		}
	}
}
