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

	public interface IRoutingService : IOpStatsAggregate, IOpStatsParent
	{
		event EventHandler<IRoutingService, RouterEventArgs> RouterEventOccurred;

		RouterRules Rules { get; }
		Status Status { get; }
		IReadOnlyList<INodeService> Nodes { get; }

		Exception LatestConnectionError { get; }

		/// <summary>
		///     Adds a node with the given settings.
		/// </summary>
		/// <param name="serviceParams">Parameters.</param>
		/// <param name="weight">
		///     The rating of this node.<br />
		///     Used with weighted algorithms to hit higher rated nodes a bit more than others.
		/// </param>
		/// <returns>A node that can be used to define a rule exception, for example.</returns>
		INodeService AddNode(EnhancedServiceParams serviceParams, int weight = 1);

		IRoutingService RemoveNode(INodeService node);

		/// <summary>
		///     Defines the rules to apply on this router.
		/// </summary>
		IRoutingService DefineRules(RouterRules rules);

		/// <summary>
		///     Defines exception rules for certain operations to be routed to certain nodes only.
		/// </summary>
		/// <param name="evaluator">Test function, which targets the given node if 'true'.</param>
		/// <param name="targetNode">Node to use when the function succeeds.</param>
		IRoutingService AddException(Func<OrganizationRequest, IEnhancedOrgService, bool> evaluator, INodeService targetNode);

		IRoutingService ClearExceptions();
		IRoutingService ClearExceptions(INodeService targetNode);

		/// <summary>
		///     Defines an ordered list of nodes to use when the current node fails an operation (after auto-retry as well).
		/// </summary>
		IRoutingService DefineFallback(IOrderedEnumerable<INodeService> fallbackNodes);

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
		///     Starts creating connections to fill the internal queues. Makes retrieving the connections a lot faster later.
		/// </summary>
		IRoutingService WarmUp();

		/// <summary>
		///     Stops the warmup process.
		/// </summary>
		IRoutingService EndWarmup();

		/// <summary>
		///     Returns the next node to use, as per the rules defined.
		/// </summary>
		IEnhancedOrgService GetService(int threads = 1);
	}
}
