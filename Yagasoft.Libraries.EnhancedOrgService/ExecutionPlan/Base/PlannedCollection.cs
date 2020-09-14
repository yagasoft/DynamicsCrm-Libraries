#region Imports

using System;
using System.Runtime.Serialization;
using Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.Planning;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.Base
{
	[DataContract]
	public abstract class PlannedCollection<TElement> : PlannedMapBase where TElement : IPlannedValue
	{
		protected PlannedCollection(Guid? parentId, string alias) : base(parentId, alias)
		{ }

		public TElement First()
		{
			return GetValue(1);
		}

		public TElement GetValue(int index)
		{
			return GetPlannedValue<TElement>(index.ToString());
		}

		public TElement this[int index] => GetValue(index);
	}
}
