#region Imports

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Xrm.Sdk;
using Yagasoft.Libraries.Common;
using Yagasoft.Libraries.EnhancedOrgService.Exceptions;
using Yagasoft.Libraries.EnhancedOrgService.Params;
using Yagasoft.Libraries.EnhancedOrgService.Response.Operations;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced;
using Yagasoft.Libraries.EnhancedOrgService.State;
using OperationStatus = Yagasoft.Libraries.EnhancedOrgService.Response.Operations.OperationStatus;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Router
{
	public class RoutingService : IStateful, IRoutingService
	{
		public RouterRules Rules { get; protected internal set; }
		public virtual bool IsRunning { get; protected internal set; }

		public virtual event EventHandler<OperationStatusEventArgs> OperationStatusChanged;
		public virtual event EventHandler<OperationFailedEventArgs> OperationFailed;

		public IOperationStats Stats => new OperationStats(NodeQueue);
		
		protected internal readonly ConcurrentQueue<NodeService> NodeQueue = new ConcurrentQueue<NodeService>();

		protected internal readonly ConcurrentDictionary<Func<OrganizationRequest, IEnhancedOrgService, bool>, INodeService> Exceptions =
			new ConcurrentDictionary<Func<OrganizationRequest, IEnhancedOrgService, bool>, INodeService>();

		protected internal IOrderedEnumerable<INodeService> FallbackNodes;

		private readonly object dequeueLock = new object();

		public RoutingService()
		{
			Rules = new RouterRules();
		}

		public virtual void ValidateState(bool isValid = true)
		{
			if (!NodeQueue.Any())
			{
				throw new StateException("At least one node must be added to the router.");
			}

			if (Rules.IsFallbackEnabled == true && FallbackNodes == null)
			{
				throw new StateException("Fallback is enabled; a fallback list must be provided.");
			}

			if (Rules.IsFallbackEnabled != true && FallbackNodes != null)
			{
				throw new StateException("Cannot define a fallback if fallback is not enabled in the rules.");
			}
		}

		public virtual INodeService AddNode(EnhancedServiceParams serviceParams, int weight = 1)
		{
			serviceParams.Require(nameof(serviceParams), "Service Parameters must be set first.");
			weight.RequireAtLeast(1, nameof(weight));

			var node = new NodeService(serviceParams, weight);

			if (NodeQueue.IsEmpty)
			{
				SetPrimaryNode(node);
			}

			NodeQueue.Enqueue(node);

			return node;
		}

		public IRoutingService SetPrimaryNode(INodeService node)
		{
			if (node is NodeService nodeService)
			{
				foreach (var service in NodeQueue)
				{
					service.IsPrimary = false;
				}

				nodeService.IsPrimary = true;
			}
			else
			{
				throw new NotSupportedException("Node type is not supported.");
			}

			return this;
		}

		public IRoutingService RemoveNode(INodeService nodeToRemove)
		{
			lock (dequeueLock)
			{
				while (NodeQueue.TryDequeue(out var node) && node != nodeToRemove)
				{
					NodeQueue.Enqueue(node);
				} 
			}

			return this;
		}

		public virtual IRoutingService DefineRules(RouterRules rules)
		{
			rules.Require(nameof(rules));

			rules.IsLocked = true;

			var backup = Rules;
			Rules = rules;

			try
			{
				ValidateState();
			}
			catch (Exception)
			{
				Rules = backup;
				throw;
			}

			foreach (var node in NodeQueue)
			{
				node.LatencyInterval = rules.NodeCheckInterval;
			}

			return this;
		}

		public virtual IRoutingService AddException(Func<OrganizationRequest, IEnhancedOrgService, bool> evaluator,
			INodeService targetNode)
		{
			throw new NotSupportedException("Exceptions are not supported yet.");

			Exceptions[evaluator] = targetNode;
			return this;
		}

		public virtual IRoutingService ClearExceptions()
		{
			throw new NotSupportedException("Exceptions are not supported yet.");

			Exceptions.Clear();
			return this;
		}

		public virtual IRoutingService ClearExceptions(INodeService targetNode)
		{
			throw new NotSupportedException("Exceptions are not supported yet.");

			Exceptions.TryRemove(Exceptions.FirstOrDefault(e => e.Value == targetNode).Key, out _);
			return this;
		}

		public virtual IRoutingService DefineFallback(IOrderedEnumerable<INodeService> fallbackNodes)
		{
			fallbackNodes.Require(nameof(fallbackNodes));

			if (fallbackNodes.Any(n => !(n is NodeService)))
			{
				throw new NotSupportedException("Node type is not supported.");
			}

			FallbackNodes = fallbackNodes;
			return this;
		}

		public virtual IRoutingService StartRouter()
		{
			ValidateState();

			foreach (var service in NodeQueue)
			{
				service.StartNode();
			}

			IsRunning = true;

			return this;
		}

		public virtual IRoutingService WarmUp()
		{
			ValidateState();

			foreach (var service in NodeQueue)
			{
				service.Pool.WarmUp();
			}

			return this;
		}

		public virtual IRoutingService EndWarmup()
		{
			ValidateState();

			foreach (var service in NodeQueue)
			{
				service.Pool.EndWarmup();
			}

			return this;
		}

		public virtual IEnhancedOrgService GetService(int threads = 1)
		{
			threads.RequireAtLeast(1);

			ValidateState();

			NodeService node = null;

			lock (dequeueLock)
			{
				while (true)
				{
					switch (Rules.RouterMode)
					{
						case RouterMode.RoundRobin:
						case null:
							foreach (var _ in NodeQueue)
							{
								NodeQueue.TryDequeue(out node);
								NodeQueue.Enqueue(node);

								if (node.Status == NodeStatus.Online)
								{
									break;
								}
							}
							break;

						case RouterMode.WeightedRoundRobin:
							foreach (var _ in NodeQueue)
							{
								var currentNode = NodeQueue.FirstOrDefault();
								var latestNode = NodeQueue.LastOrDefault();
								var currentNodeExecutions = currentNode?.Pool.Stats.RequestCount;
								var latestNodeExecutions = latestNode?.Pool.Stats.RequestCount;

								if (currentNode?.Status == NodeStatus.Online
									&& (currentNodeExecutions / (double?)latestNodeExecutions) < (currentNode?.Weight / (double?)latestNode?.Weight))
								{
									node = currentNode;
								}
								else
								{
									NodeQueue.TryDequeue(out node);
									NodeQueue.Enqueue(node);
									node = NodeQueue.FirstOrDefault();
								}

								if (node?.Status == NodeStatus.Online)
								{
									break;
								}
							}
							break;

						case RouterMode.StaticWithFallback:
							node = NodeQueue.FirstOrDefault(n => n.IsPrimary)
								?? NodeQueue.FirstOrDefault(n => n.Status == NodeStatus.Online);
							break;

						case RouterMode.LeastLoaded:
							node = NodeQueue
								.Select(n =>
									new
									{
										n,
										load = n.Pool.Stats.PendingOperations
											.Count(o => o.OperationStatus != OperationStatus.Success
												&& o.OperationStatus != OperationStatus.Failure)
									})
								.Where(e => e.n.Status == NodeStatus.Online)
								.OrderBy(e => e.load)
								.FirstOrDefault()?.n;
							break;

						case RouterMode.LeastLatency:
							node = NodeQueue
								.Where(n => n.Status == NodeStatus.Online)
								.OrderBy(n => n.Latency).FirstOrDefault();
							break;

						default:
							throw new ArgumentOutOfRangeException(nameof(Rules.RouterMode), Rules.RouterMode, "Router mode is not supported.");
					}

					if (Rules.IsFallbackEnabled == true)
					{
						node ??= FallbackNodes.OfType<NodeService>().Union(NodeQueue).FirstOrDefault(n => n.Status == NodeStatus.Online);
					}

					// if no nodes found and there is a node with no latency measured (starting up), wait
					if (node == null && NodeQueue.Union(FallbackNodes.OfType<NodeService>()).Any(n => !n.LatencyHistory.Any()))
					{
						Thread.Sleep(100);
						continue;
					}

					break;
				}
			}

			if (node == null)
			{
				throw new NodeSelectException("Cannot find a valid node.");
			}

			var service = node.Pool.GetService(threads);

			if (service.Parameters.AutoRetryParams?.CustomRetryFunctions.Contains(CustomRetry) == false)
			{
				service.Parameters.AutoRetryParams.CustomRetryFunctions?.Add(CustomRetry);
			}

			return service;
		}

		protected internal virtual object CustomRetry(Func<IOrganizationService, object> action, Operation operation,
			ExecuteParams executeParams, Exception ex)
		{
			foreach (var fallbackNode in FallbackNodes)
			{
				try
				{
					return (fallbackNode.Pool.GetService() as EnhancedOrgServiceBase)?.TryRunOperation(action, operation, executeParams);
				}
				catch
				{
					// ignored
				}
			}

			return null;
		}
	}
}
