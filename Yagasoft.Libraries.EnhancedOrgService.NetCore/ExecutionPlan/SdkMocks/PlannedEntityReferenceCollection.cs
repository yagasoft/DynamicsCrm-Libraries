#region Imports

using System;
using System.Runtime.Serialization;
using Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.Base;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.SdkMocks
{
	[DataContract]
	internal class PlannedEntityReferenceCollection : PlannedCollection<PlannedEntityReference>
	{
		internal PlannedEntityReferenceCollection(Guid? parentId, string alias) : base(parentId, alias)
		{ }
	}
}
