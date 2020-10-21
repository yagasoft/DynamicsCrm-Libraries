#region Imports

using System;
using Yagasoft.Libraries.EnhancedOrgService.Operations;
using Yagasoft.Libraries.EnhancedOrgService.Params;
using Yagasoft.Libraries.EnhancedOrgService.Pools;
using Yagasoft.Libraries.EnhancedOrgService.Response.Operations;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Router
{
	public enum NodeStatus
	{
		Unknown,
		Online,
		Offline
	}

	public interface INodeService : IOpStatsAggregate, IOpStatsParent
	{
		EnhancedServiceParams Params { get; }
		int Weight { get; }
		IEnhancedServicePool<IEnhancedOrgService> Pool { get; }
		NodeStatus Status { get; }
		bool IsPrimary { get; }

		TimeSpan Latency { get; }
		DateTime? Started { get; }
		TimeSpan? Uptime { get; }
		TimeSpan? Downtime { get; }
		double UpPercent { get; }
	}
}
