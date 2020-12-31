#region Imports

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Yagasoft.Libraries.Common;
using Yagasoft.Libraries.EnhancedOrgService.Events;
using Yagasoft.Libraries.EnhancedOrgService.Events.EventArgs;
using Yagasoft.Libraries.EnhancedOrgService.Exceptions;
using Yagasoft.Libraries.EnhancedOrgService.Helpers;
using Yagasoft.Libraries.EnhancedOrgService.Operations;
using Yagasoft.Libraries.EnhancedOrgService.Params;
using Yagasoft.Libraries.EnhancedOrgService.Pools;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Router.Node
{
	public class NodeService : INodeService
	{
		public virtual event EventHandler<INodeService, NodeStatusEventArgs> NodeStatusChanged
		{
			add
			{
				InnerNodeStatusChanged -= value;
				InnerNodeStatusChanged += value;
			}
			remove => InnerNodeStatusChanged -= value;
		}
		protected virtual event EventHandler<INodeService, NodeStatusEventArgs> InnerNodeStatusChanged;
		
		public EnhancedServiceParams Params { get; protected internal set; }
		public int Weight { get; protected internal set; }
		public virtual IEnhancedServicePool<IEnhancedOrgService> Pool { get; protected internal set; }

		public virtual NodeStatus Status
		{
			get => status;
			protected internal set
			{
				status = value;
				OnStatusChanged();
			}
		}

		public virtual bool IsPrimary { get; protected internal set; }

		public virtual Exception LatestConnectionError { get; protected internal set; }

		public virtual TimeSpan Latency => LatencyHistory.Any()
			? TimeSpan.FromMilliseconds(LatencyHistory.Average(e => e.TotalMilliseconds))
			: TimeSpan.MaxValue;

		public virtual DateTime? Started { get; protected internal set; }
		public virtual TimeSpan? Uptime => DateTime.Now - Started - Downtime;
		public virtual TimeSpan? Downtime { get; protected internal set; }

		public virtual double UpPercent => (DateTime.Now - Started).GetValueOrDefault().TotalMilliseconds
			/ Uptime.GetValueOrDefault(TimeSpan.FromTicks(1)).TotalMilliseconds;

		public IOperationStats Stats { get; }

		public virtual IEnumerable<IOperationStats> StatTargets => Pool == null ? new IOperationStats[0] : new[] { Pool.Stats };

		protected internal Thread LatencyEvaluator;
		protected internal IOrganizationService LatencyEvaluatorService;
		protected internal TimeSpan? LatencyInterval;
		protected internal FixedSizeQueue<TimeSpan> LatencyHistory = new(5);
		private NodeStatus status;

		protected internal NodeService(EnhancedServiceParams @params, int weight = 1)
		{
			Stats = new OperationStats(this);

			Params = @params;
			Weight = weight;

			LatencyEvaluator =
				new Thread(
					() =>
					{
						var downtime = new Stopwatch();

						while (Status != NodeStatus.Offline)
						{
							try
							{
								var stopwatch = new Stopwatch();
								stopwatch.Start();

								if (Status != NodeStatus.Online)
								{
									Downtime += downtime.Elapsed;
								}

								downtime.Reset();
								downtime.Start();

								var thread =
									new Thread(
										() =>
										{
											try
											{
												LatencyEvaluatorService.Execute(new WhoAmIRequest());
											}
											catch (ThreadAbortException)
											{ }
										}) { IsBackground = true };
								thread.Start();

								if (!thread.Join(TimeSpan.FromSeconds(10)))
								{
									thread.Abort();
									LatencyHistory.Enqueue(TimeSpan.MaxValue);
									Status = NodeStatus.Unknown;
									LatencyEvaluatorService.Execute(new WhoAmIRequest());
								}

								stopwatch.Stop();

								LatencyHistory.Enqueue(stopwatch.Elapsed);
								LatestConnectionError = null;
								Status = NodeStatus.Online;
							}
							catch (Exception ex)
							{
								LatencyHistory.Enqueue(TimeSpan.MaxValue);
								LatestConnectionError = ex;
								Status = NodeStatus.Faulty;
							}
							finally
							{
								Thread.Sleep((int?)LatencyInterval?.TotalMilliseconds ?? 10000);
							}
						}

						LatencyEvaluator = null;
						LatencyEvaluatorService = null;
					}) { IsBackground = true };
		}

		protected internal virtual void StartNode()
		{
			try
			{
				Status = NodeStatus.Starting;
				LatestConnectionError = null;
				Pool = EnhancedServiceHelper.GetPool(Params);
				(Stats as OperationStats)?.Propagate();
				LatencyEvaluatorService = Pool.Factory.CreateCrmService();
				LatencyEvaluator.Start();
				Started = DateTime.Now;
				Downtime = TimeSpan.Zero;
			}
			catch (Exception ex)
			{
				LatestConnectionError = ex;
				throw new NodeInitException("Failed to start node.", ex, this);
			}
		}

		protected internal virtual void StopNode()
		{
			try
			{
				status = NodeStatus.Offline;
				Status = NodeStatus.Offline;
				Pool = null;
			}
			catch
			{
				// ignored
			}
		}
		
		protected virtual void OnStatusChanged()
		{
			InnerNodeStatusChanged?.Invoke(this, new NodeStatusEventArgs(this, Status, LatestConnectionError));
		}
	}
}
