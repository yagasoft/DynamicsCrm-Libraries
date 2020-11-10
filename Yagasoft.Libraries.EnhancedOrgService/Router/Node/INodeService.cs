#region Imports

using System;
using Yagasoft.Libraries.EnhancedOrgService.Events;
using Yagasoft.Libraries.EnhancedOrgService.Events.EventArgs;
using Yagasoft.Libraries.EnhancedOrgService.Operations;
using Yagasoft.Libraries.EnhancedOrgService.Params;
using Yagasoft.Libraries.EnhancedOrgService.Pools;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Router.Node
{
	public enum NodeStatus
	{
		Offline,
		Starting,
		Unknown,
		Online,
		Faulty
	}

	public interface INodeService : IOpStatsAggregate, IOpStatsParent
	{
		event EventHandler<INodeService, NodeStatusEventArgs> NodeStatusChanged;

		EnhancedServiceParams Params { get; }
		int Weight { get; }
		IEnhancedServicePool<IEnhancedOrgService> Pool { get; }
		NodeStatus Status { get; }
		bool IsPrimary { get; }

		Exception LatestConnectionError { get; }

		TimeSpan Latency { get; }
		DateTime? Started { get; }
		TimeSpan? Uptime { get; }
		TimeSpan? Downtime { get; }
		double UpPercent { get; }
	}
}
