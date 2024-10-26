#region Imports

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Yagasoft.Libraries.EnhancedOrgService.Events;
using Yagasoft.Libraries.EnhancedOrgService.Events.EventArgs;
using Yagasoft.Libraries.EnhancedOrgService.Operations;
using Yagasoft.Libraries.EnhancedOrgService.Params;
using Yagasoft.Libraries.EnhancedOrgService.Pools;
using Yagasoft.Libraries.EnhancedOrgService.Response.Operations;
using Yagasoft.Libraries.EnhancedOrgService.Router.Node;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Router
{
	public enum Status
	{
		Offline,
		Starting,
		Failed,
		Online,
		Stopping
	}

	public interface IRoutingService<TService> : IOpStatsAggregate, IOpStatsParent
		where TService : IOrganizationService
	{
		event EventHandler<IRoutingService<TService>, RouterEventArgs> RouterEventOccurred;

		RouterRules Rules { get; }
		Status Status { get; }
		IReadOnlyList<INodeService> Nodes { get; }

		Exception LatestConnectionError { get; }

		/// <summary>
		///     Adds a node with the given settings.
		/// </summary>
		/// <param name="pool">Pool to get services from for this node.</param>
		/// <param name="weight">
		///     The rating of this node.<br />
		///     Used with weighted algorithms to hit higher rated nodes a bit more than others.
		/// </param>
		/// <returns>A node that can be used to define a rule exception, for example.</returns>
		INodeService AddNode(IServicePool<TService> pool, int weight = 1);

		Task<IRoutingService<TService>> RemoveNode(INodeService node);

		/// <summary>
		///     Defines the rules to apply on this router.
		/// </summary>
		IRoutingService<TService> DefineRules(RouterRules rules);

		/// <summary>
		///     Defines exception rules for certain operations to be routed to certain nodes only.
		/// </summary>
		/// <param name="evaluator">Test function, which targets the given node if 'true'.</param>
		/// <param name="targetNode">Node to use when the function succeeds.</param>
		IRoutingService<TService> AddException(Func<OrganizationRequest, TService, bool> evaluator, INodeService targetNode);

		IRoutingService<TService> ClearExceptions();
		IRoutingService<TService> ClearExceptions(INodeService targetNode);

		/// <summary>
		///     Defines an ordered list of nodes to use when the current node fails an operation (after auto-retry as well).
		/// </summary>
		IRoutingService<TService> DefineFallback(IOrderedEnumerable<INodeService> fallbackNodes);

		/// <summary>
		///     Validates the router configuration state, and then initialises all internal nodes.<br />
		///     This operation is done asynchronously.
		/// </summary>
		Task StartRouter();

		/// <summary>
		///     Stops all internal nodes.
		/// </summary>
		void StopRouter();

		/// <summary>
		///     Returns the next node to use, as per the rules defined.
		/// </summary>
		Task<TService> GetService();
	}
}
