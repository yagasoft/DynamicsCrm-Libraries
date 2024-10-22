#region Imports

using System;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.Planning
{
	public interface IPlannedValue
	{
		Guid Id { get; }
		string Alias { get; }
		Guid? ParentId { get; }
	}
}
