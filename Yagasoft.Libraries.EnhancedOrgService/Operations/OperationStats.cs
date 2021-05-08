#region Imports

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Yagasoft.Libraries.EnhancedOrgService.Events;
using Yagasoft.Libraries.EnhancedOrgService.Events.EventArgs;
using Yagasoft.Libraries.EnhancedOrgService.Response.Operations;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Operations
{
	public class OperationStats : IOperationStats
	{
		public virtual event EventHandler<IOrganizationService, OperationStatusEventArgs> OperationStatusChanged
		{
			add
			{
				OperationStatusChanged -= value;
				InnerOperationStatusChanged += value;

				if (TargetStatsParent?.StatTargets != null)
				{
					foreach (var target in TargetStatsParent.StatTargets)
					{
						target.OperationStatusChanged += value;
					}
				}
			}
			remove
			{
				InnerOperationStatusChanged -= value;

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

		public virtual int RequestCount => TargetStatsParent?.StatTargets?.Sum(t => t.RequestCount) ?? -1;

		public virtual int FailureCount => TargetStatsParent?.StatTargets?.Sum(t => t.FailureCount) ?? -1;

		public virtual double FailureRate => TargetStatsParent?.StatTargets?.Sum(t => t.FailureRate) ?? -1;

		public virtual int RetryCount => TargetStatsParent?.StatTargets?.Sum(t => t.RetryCount) ?? -1;

		public virtual IEnumerable<Operation> PendingOperations => TargetStatsParent?.StatTargets?.SelectMany(t => t.PendingOperations) ?? Array.Empty<Operation>();

		public virtual IEnumerable<Operation> ExecutedOperations => TargetStatsParent?.StatTargets?.SelectMany(t => t.ExecutedOperations) ?? Array.Empty<Operation>();

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
					.OfType<EventHandler<IOrganizationService, OperationStatusEventArgs>>().ToArray())
				{
					OperationStatusChanged += invocation;
				}
			}
		}
	}
}
