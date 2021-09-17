#region Imports

using System;
using System.Runtime.Serialization;
using Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.Base;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.SdkMocks
{
	[DataContract]
	public class PlannedOptionSetValueCollection : PlannedCollection<PlannedOptionSetValue>
	{
		public PlannedOptionSetValueCollection(Guid? parentId, string alias) : base(parentId, alias)
		{ }
	}
}
