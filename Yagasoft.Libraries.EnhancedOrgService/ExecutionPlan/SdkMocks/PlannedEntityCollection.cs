#region Imports

using System;
using System.Runtime.Serialization;
using Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.Base;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.SdkMocks
{
	[DataContract]
	public class PlannedEntityCollection : PlannedCollection<PlannedEntity>
	{
		internal PlannedEntityCollection(Guid? parentId, string alias) : base(parentId, alias)
		{ }
	}
}
