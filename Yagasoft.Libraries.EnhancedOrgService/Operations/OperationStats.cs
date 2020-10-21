#region Imports

using System;
using System.Collections.Generic;
using System.Linq;
using Yagasoft.Libraries.EnhancedOrgService.Events;
using Yagasoft.Libraries.EnhancedOrgService.Events.EventArgs;
using Yagasoft.Libraries.EnhancedOrgService.Response.Operations;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Operations
{
	public class OperationStats : IOperationStats
	{
		public virtual event EventHandler<IEnhancedOrgService, OperationStatusEventArgs> OperationStatusChanged
		{
			add
			{
				InnerOperationStatusChanged -= value;
				InnerOperationStatusChanged += value;
			}
			remove
			{
				InnerOperationStatusChanged -= value;

				if (TargetStatsParent?.Containers != null)
				{
					foreach (var container in TargetStatsParent.Containers.Select(t => new OperationStats(t)))
					{
						container.OperationStatusChanged -= value;
					}
				}

				if (TargetStatsParent?.StatTargets != null)
				{
					foreach (var target in TargetStatsParent.StatTargets)
					{
						target.OperationStatusChanged -= value;
					}
				}
			}
		}

		private event EventHandler<IEnhancedOrgService, OperationStatusEventArgs> InnerOperationStatusChanged;

		public virtual event EventHandler<IEnhancedOrgService, OperationFailedEventArgs> OperationFailed
		{
			add
			{
				InnerOperationFailed -= value;
				InnerOperationFailed += value;
			}
			remove
			{
				InnerOperationFailed -= value;

				if (TargetStatsParent?.Containers != null)
				{
					foreach (var container in TargetStatsParent.Containers.Select(t => new OperationStats(t)))
					{
						container.OperationFailed -= value;
					}
				}

				if (TargetStatsParent?.StatTargets != null)
				{
					foreach (var target in TargetStatsParent.StatTargets)
					{
						target.OperationFailed -= value;
					}
				}
			}
		}

		private event EventHandler<IEnhancedOrgService, OperationFailedEventArgs> InnerOperationFailed;

		public virtual int RequestCount => TargetStatsParent?.Containers?.Sum(t => new OperationStats(t).RequestCount)
			?? TargetStatsParent?.StatTargets?.Sum(t => t.RequestCount) ?? -1;

		public virtual int FailureCount => TargetStatsParent?.Containers?.Sum(t => new OperationStats(t).FailureCount)
			?? TargetStatsParent?.StatTargets?.Sum(t => t.FailureCount) ?? -1;

		public virtual double FailureRate => TargetStatsParent?.Containers?.Sum(t => new OperationStats(t).FailureRate)
			?? TargetStatsParent?.StatTargets?.Sum(t => t.FailureRate) ?? -1;

		public virtual int RetryCount => TargetStatsParent?.Containers?.Sum(t => new OperationStats(t).RetryCount)
			?? TargetStatsParent?.StatTargets?.Sum(t => t.RetryCount) ?? -1;

		public virtual IEnumerable<Operation> PendingOperations =>
			TargetStatsParent?.Containers?.SelectMany(t => new OperationStats(t).PendingOperations)
				?? TargetStatsParent?.StatTargets?.SelectMany(t => t.PendingOperations) ?? Array.Empty<Operation>();

		public virtual IEnumerable<Operation> ExecutedOperations =>
			TargetStatsParent?.Containers?.SelectMany(t => new OperationStats(t).ExecutedOperations)
				?? TargetStatsParent?.StatTargets?.SelectMany(t => t.ExecutedOperations) ?? Array.Empty<Operation>();

		protected internal IOpStatsParent TargetStatsParent;

		protected internal OperationStats(IOpStatsParent statsParent)
		{
			TargetStatsParent = statsParent;
		}

		protected internal void Propagate()
		{
			if (InnerOperationStatusChanged != null)
			{
				foreach (var invocation in InnerOperationStatusChanged.GetInvocationList()
					.OfType<EventHandler<IEnhancedOrgService, OperationStatusEventArgs>>().ToArray())
				{
					OperationStatusChanged += invocation;
				}
			}

			if (InnerOperationFailed != null)
			{
				foreach (var invocation in InnerOperationFailed.GetInvocationList()
					.OfType<EventHandler<IEnhancedOrgService, OperationFailedEventArgs>>().ToArray())
				{
					OperationFailed += invocation;
				}
			}
		}
	}
}
