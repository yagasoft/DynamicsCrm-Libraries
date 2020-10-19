#region Imports

using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Response.Operations
{
	public class OperationStats : IOperationStats
	{
		public virtual int RequestCount => TargetContainers == null
			? (Targets?.Sum(t => t.RequestCount) ?? throw new ArgumentNullException(nameof(Targets)))
			: new OperationStats(TargetContainers.Select(t => t.Stats)).RequestCount;

		public virtual int FailureCount => TargetContainers == null
			? (Targets?.Sum(t => t.FailureCount) ?? throw new ArgumentNullException(nameof(Targets)))
			: new OperationStats(TargetContainers.Select(t => t.Stats)).FailureCount;

		public virtual double FailureRate => TargetContainers == null
			? (Targets?.Sum(t => t.FailureRate) ?? throw new ArgumentNullException(nameof(Targets)))
			: new OperationStats(TargetContainers.Select(t => t.Stats)).FailureRate;

		public virtual int RetryCount => TargetContainers == null
			? (Targets?.Sum(t => t.RetryCount) ?? throw new ArgumentNullException(nameof(Targets)))
			: new OperationStats(TargetContainers.Select(t => t.Stats)).RetryCount;

		public virtual IEnumerable<Operation> PendingOperations => TargetContainers == null
			? (Targets?.SelectMany(t => t.PendingOperations) ?? throw new ArgumentNullException(nameof(Targets)))
			: new OperationStats(TargetContainers.Select(t => t.Stats)).PendingOperations;

		public virtual IEnumerable<Operation> ExecutedOperations => TargetContainers == null
			? (Targets?.SelectMany(t => t.ExecutedOperations) ?? throw new ArgumentNullException(nameof(Targets)))
			: new OperationStats(TargetContainers.Select(t => t.Stats)).ExecutedOperations;

		protected internal IEnumerable<IOpStatsContainer> TargetContainers;
		protected internal IEnumerable<IOperationStats> Targets;

		protected internal OperationStats(IEnumerable<IOpStatsContainer> targets)
		{
			TargetContainers = targets;
		}

		protected internal OperationStats(IEnumerable<IOperationStats> targets)
		{
			Targets = targets;
		}
	}
}
