#region Imports

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Yagasoft.Libraries.Common;
using Yagasoft.Libraries.EnhancedOrgService.Events;
using Yagasoft.Libraries.EnhancedOrgService.Events.EventArgs;
using Yagasoft.Libraries.EnhancedOrgService.Exceptions;
using Yagasoft.Libraries.EnhancedOrgService.Operations;
using Yagasoft.Libraries.EnhancedOrgService.Params;
using Yagasoft.Libraries.EnhancedOrgService.Response.Operations;
using Yagasoft.Libraries.EnhancedOrgService.Router.Node;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced;
using Yagasoft.Libraries.EnhancedOrgService.State;
using NodeStatus = Yagasoft.Libraries.EnhancedOrgService.Router.Node.NodeStatus;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Router
{
	public class RoutingService : IStateful, IRoutingService
	{
		public virtual event EventHandler<IRoutingService, RouterEventArgs> RouterEventOccurred
		{
			add
			{
				InnerRouterEventOccurred -= value;
				InnerRouterEventOccurred += value;
			}
			remove => InnerRouterEventOccurred -= value;
		}
		protected virtual event EventHandler<IRoutingService, RouterEventArgs> InnerRouterEventOccurred;
		
		public RouterRules Rules { get; protected internal set; }

		public Status Status
		{
			get => status;
			protected internal set
			{
				status = value;
				OnEventOccurred(Event.StatusChanged, LatestFaultyNode);
			}
		}

		public IOperationStats Stats { get; }

		public virtual IEnumerable<IOperationStats> StatTargets => NodeQueue.Select(n => n.Stats);

		public virtual IReadOnlyList<INodeService> Nodes => NodeQueue.ToArray();

		public virtual Exception LatestConnectionError { get; protected internal set; }

		protected internal readonly ConcurrentQueue<NodeService> NodeQueue = new();

		protected internal readonly ConcurrentDictionary<Func<OrganizationRequest, IEnhancedOrgService, bool>, INodeService> Exceptions
			= new();

		protected internal IOrderedEnumerable<INodeService> FallbackNodes;

		protected internal virtual INodeService LatestFaultyNode { get; set; }

		private readonly object dequeueLock = new();
		private Status status;

		public RoutingService()
		{
			Stats = new OperationStats(this);
			Rules = new RouterRules();
			Status = Status.Offline;
		}

		public virtual void ValidateState(bool isValid = true)
		{
			if (Status != Status.Offline)
			{
				throw new StateException("Router is not offline.");
			}

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

			if (Status != Status.Offline)
			{
				throw new StateException("Router is not stopped.");
			}

			var node = new NodeService(serviceParams, weight);

			if (NodeQueue.IsEmpty)
			{
				SetPrimaryNode(node);
			}

			NodeQueue.Enqueue(node);

			foreach (var nodeService in NodeQueue)
			{
				nodeService.NodeStatusChanged += (s, a) => OnEventOccurred(Event.NodeStatusChanged, s);
			}

			OnEventOccurred(Event.NodeQueueAdded, node);
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
			ValidateState();

			lock (dequeueLock)
			{
				while (NodeQueue.TryDequeue(out var node) && node != nodeToRemove)
				{
					NodeQueue.Enqueue(node);
				}
			}

			OnEventOccurred(Event.NodeQueueRemoved, nodeToRemove);
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

		public virtual async Task StartRouter()
		{
			ValidateState();

			Status = Status.Starting;
			(Stats as OperationStats)?.Propagate();

			LatestConnectionError = null;

			void NodeEvent(INodeService s, NodeStatusEventArgs a)
			{
				lock (NodeQueue)
				{
					if (s.Status != NodeStatus.Starting)
					{
						s.NodeStatusChanged -= NodeEvent;
					}

					if (NodeQueue.Any(n => n.Status == NodeStatus.Starting) || Status != Status.Starting)
					{
						return;
					}

					Status = Status.Online;
				}
			}

			foreach (var node in NodeQueue)
			{
				await Task.Factory.StartNew(
					() =>
					{
						try
						{
							node.NodeStatusChanged += NodeEvent;
							node.StartNode();
						}
						catch
						{
							LatestConnectionError = node.LatestConnectionError;
							LatestFaultyNode = node;
							Status = Status.Failed;
							throw;
						}
					}, TaskCreationOptions.LongRunning).ConfigureAwait(false);
			}
		}

		public virtual void StopRouter()
		{
			if (Status != Status.Online)
			{
				throw new StateException("Router is not online.");
			}

			Status = Status.Stopping;

			foreach (var node in NodeQueue)
			{
				node.StopNode();
			}

			Status = Status.Offline;
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
			foreach (var service in NodeQueue)
			{
				service.Pool.EndWarmup();
			}

			return this;
		}

		public virtual IEnhancedOrgService GetService(int threads = 1)
		{
			threads.RequireAtLeast(1);

			for (var i = 0; i < (60 * 1000 / 100) && Status == Status.Starting; i++)
			{
				Thread.Sleep(100);
			}

			if (Status != Status.Online)
			{
				throw new StateException("Router must be running.");
			}

			NodeService node;

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

							node = null;

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

							node = null;

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
											.Count(o => o.OperationStatus != Response.Operations.Status.Success
												&& o.OperationStatus != Response.Operations.Status.Failure)
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
						node ??= FallbackNodes?.OfType<NodeService>().Union(NodeQueue).FirstOrDefault(n => n.Status == NodeStatus.Online);
					}

					node ??= NodeQueue.FirstOrDefault(n => n.Status == NodeStatus.Online);

					break;
				}
			}

			if (node == null)
			{
				OnEventOccurred(Event.RoutingFailed);
				throw new NodeSelectException("Cannot find a valid node.");
			}

			var service = node.Pool.GetService(threads);

			if (Rules.IsFallbackEnabled == true
				&& service.Parameters.AutoRetryParams?.CustomRetryFunctions.Contains(CustomRetry) == false)
			{
				service.Parameters.AutoRetryParams.CustomRetryFunctions?.Add(CustomRetry);
			}

			return service;
		}

		protected internal virtual object CustomRetry(Func<IOrganizationService, object> action, Operation operation,
			ExecuteParams executeParams, Exception ex)
		{
			FallbackNodes.Require(nameof(FallbackNodes));

			foreach (var fallbackNode in FallbackNodes)
			{
				try
				{
					using var service  = fallbackNode.Pool.GetService() as EnhancedOrgServiceBase;

					if (service == null)
					{
						continue;
					}

					return service.TryRunOperation(action, operation, executeParams, true);
				}
				catch
				{
					// ignored
				}
			}

			return null;
		}
		
		protected virtual void OnEventOccurred(Event @event, INodeService node = null)
		{
			InnerRouterEventOccurred?.Invoke(this, new RouterEventArgs(@event, Status, Rules?.RouterMode, node,
				LatestConnectionError));
		}
	}
}
