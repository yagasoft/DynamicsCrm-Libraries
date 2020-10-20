#region Imports

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Yagasoft.Libraries.Common;
using Yagasoft.Libraries.EnhancedOrgService.Exceptions;
using Yagasoft.Libraries.EnhancedOrgService.Helpers;
using Yagasoft.Libraries.EnhancedOrgService.Params;
using Yagasoft.Libraries.EnhancedOrgService.Pools;
using Yagasoft.Libraries.EnhancedOrgService.Response.Operations;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Router
{
	public class NodeService : INodeService
	{
		public EnhancedServiceParams Params { get; protected internal set; }
		public int Weight { get; protected internal set; }
		public virtual IEnhancedServicePool<IEnhancedOrgService> Pool { get; protected internal set; }
		public virtual NodeStatus Status { get; protected internal set; }
		public virtual bool IsPrimary { get; protected internal set; }

		public virtual TimeSpan Latency => LatencyHistory.Any()
			? TimeSpan.FromMilliseconds(LatencyHistory.Average(e => e.TotalMilliseconds))
			: TimeSpan.MaxValue;

		public virtual DateTime? Started { get; protected internal set; }
		public virtual TimeSpan? Uptime => DateTime.Now - Started - Downtime;
		public virtual TimeSpan? Downtime { get; protected internal set; }

		public virtual double UpPercent => (DateTime.Now - Started).GetValueOrDefault().TotalMilliseconds
			/ Uptime.GetValueOrDefault(TimeSpan.FromTicks(1)).TotalMilliseconds;

		public IOperationStats Stats => new OperationStats(this);

		public virtual IEnumerable<IOpStatsParent> Containers => new[] { Pool };
		public virtual IEnumerable<IOperationStats> StatTargets => null;

		protected internal Thread LatencyEvaluator;
		protected internal IOrganizationService LatencyEvaluatorService;
		protected internal TimeSpan? LatencyInterval;
		protected internal FixedSizeQueue<TimeSpan> LatencyHistory = new FixedSizeQueue<TimeSpan>(5);

		protected internal NodeService(EnhancedServiceParams @params, int weight = 1)
		{
			Params = @params;
			Weight = weight;

			LatencyEvaluator =
				new Thread(
					() =>
					{
						var downtime = new Stopwatch();

						while (true)
						{
							try
							{
								var stopwatch = new Stopwatch();
								stopwatch.Start();

								if (Status == NodeStatus.Offline)
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
										});

								if (!thread.Join(TimeSpan.FromSeconds(10)))
								{
									thread.Abort();
									LatencyHistory.Enqueue(TimeSpan.MaxValue);
									Status = NodeStatus.Unknown;
									LatencyEvaluatorService.Execute(new WhoAmIRequest());
								}

								stopwatch.Stop();

								LatencyHistory.Enqueue(stopwatch.Elapsed);
								Status = NodeStatus.Online;
							}
							catch
							{
								LatencyHistory.Enqueue(TimeSpan.MaxValue);
								Status = NodeStatus.Offline;
							}
							finally
							{
								Thread.Sleep((int?)LatencyInterval?.TotalMilliseconds ?? 10000);
							}
						}
					});
		}

		protected internal virtual void StartNode()
		{
			try
			{
				Pool = EnhancedServiceHelper.GetPool(Params);
				LatencyEvaluatorService = Pool.Factory.CreateCrmService();
				LatencyEvaluator.Start();
				Started = DateTime.Now;
				Downtime = TimeSpan.Zero;
			}
			catch (Exception ex)
			{
				throw new NodeInitException("Failed to start node.", ex, this);
			}
		}
	}
}
