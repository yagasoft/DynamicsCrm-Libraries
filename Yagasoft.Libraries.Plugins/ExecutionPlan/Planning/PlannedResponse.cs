#region Imports

using System;
using System.Runtime.Serialization;
using Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.Base;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.Planning
{
	[DataContract]
	public class PlannedResponse : PlannedMapBase
	{
		public PlannedResponse(Guid? parentId = null, string alias = null) : base(parentId, alias)
		{ }
	}
}
