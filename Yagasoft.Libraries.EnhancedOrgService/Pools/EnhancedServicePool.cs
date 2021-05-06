#region Imports

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Xrm.Sdk;
using Yagasoft.Libraries.Common;
using Yagasoft.Libraries.EnhancedOrgService.Factories;
using Yagasoft.Libraries.EnhancedOrgService.Helpers;
using Yagasoft.Libraries.EnhancedOrgService.Operations;
using Yagasoft.Libraries.EnhancedOrgService.Params;
using Yagasoft.Libraries.EnhancedOrgService.Response.Operations;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced.Transactions;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Pools
{
	/// <inheritdoc cref="IEnhancedServicePool{TService}" />
	public class EnhancedServicePool<TServiceInterface, TEnhancedOrgService> : IEnhancedServicePool<TServiceInterface>
		where TServiceInterface : IEnhancedOrgService
		where TEnhancedOrgService : EnhancedOrgServiceBase, TServiceInterface
	{
		public IOperationStats Stats { get; }

		public virtual IEnumerable<IOperationStats> StatTargets => statServices;

		public IEnhancedServiceFactory<TServiceInterface> Factory => factory;

		private readonly EnhancedServiceFactory<TServiceInterface, TEnhancedOrgService> factory;

		private readonly BlockingQueue<TServiceInterface> servicesQueue = new();
		private readonly ConcurrentQueue<IOrganizationService> crmServicesQueue = new();

		private readonly HashSet<IOperationStats> statServices = new();

		private readonly PoolParams poolParams;
		private int createdCrmServicesCount;

		private WarmUp.WarmUp warmUp;

		public EnhancedServicePool(EnhancedServiceFactory<TServiceInterface, TEnhancedOrgService> factory, int poolSize)
		{
			Stats = new OperationStats(this);

			this.factory = factory;
			poolParams = new PoolParams { PoolSize = poolSize };
		}

		public EnhancedServicePool(EnhancedServiceFactory<TServiceInterface, TEnhancedOrgService> factory, PoolParams poolParams = null)
		{
			Stats = new OperationStats(this);

			this.factory = factory;

			if (poolParams != null)
			{
				poolParams.IsLocked = true;
			}

			this.poolParams = poolParams ?? factory.Parameters?.PoolParams ?? new PoolParams();

		    if (this.poolParams.IsAutoPoolWarmUp == true)
		    {
		        WarmUp();
		    }
		}

		public int CreatedServices { get; private set; }
		public int CurrentPoolSize => servicesQueue.Count;

		public TServiceInterface GetService(int threads = 1)
		{
			threads.RequireAtLeast(1);
			servicesQueue.TryTake(out var service);
			return GetInitialisedService(threads, service);
		}

	    public void WarmUp()
	    {
	        lock (this)
	        {
	            warmUp ??= new WarmUp.WarmUp(
	                () =>
                    {
                        if (createdCrmServicesCount < poolParams.PoolSize)
                        {
                            crmServicesQueue.Enqueue(GetCrmService(true));
                        }
                        else
                        {
                            EndWarmup();
                        }
                    });
	        }

	        warmUp.Start();
	    }

	    public void EndWarmup()
		{
	        warmUp.End();
		}

		private IOrganizationService GetCrmService(bool isSkipQueue = false)
		{
			IOrganizationService crmService = null;

			if (!isSkipQueue)
			{
				crmServicesQueue.TryDequeue(out crmService);
			}

			if (crmService.EnsureTokenValid(poolParams.TokenExpiryCheckSecs ?? 600) == false)
			{
				crmService = null;
			}

			if (crmService != null)
			{
				return crmService;
			}

			crmService = factory.CreateCrmService();
			createdCrmServicesCount++;

			return crmService;
		}

		private TServiceInterface GetInitialisedService(int threads, [Optional] TServiceInterface enhancedService)
		{
			if (enhancedService == null)
			{
				lock (servicesQueue)
				{
					if (CreatedServices < poolParams.PoolSize)
					{
						enhancedService = GetEnhancedService();
					}
				}
			}

			try
			{
				enhancedService ??= servicesQueue.Dequeue(poolParams.DequeueTimeout);
			}
			catch (TimeoutException)
			{
				lock (servicesQueue)
				{
					enhancedService = GetEnhancedService();
				}
			}

			if (enhancedService is EnhancedOrgServiceBase enhancedOrgServiceBase)
			{
				enhancedOrgServiceBase.ReleaseService = () => ReleaseService(enhancedService);

				try
				{
			        enhancedOrgServiceBase.MaxConnectionCount = threads;
                    enhancedOrgServiceBase.CreateCrmService = () => GetCrmService();

				    enhancedOrgServiceBase.InitialiseConnectionQueue(
				        Enumerable.Range(0, threads)
				            .Select(_ => crmServicesQueue.TryDequeue(out var crmService) ? crmService : null)
				            .Where(s => s != null));
				}
				catch
				{
					enhancedOrgServiceBase.ReleaseService();
					throw;
				}
			}

			if (enhancedService is EnhancedOrgServiceBase eventService)
			{
				statServices.Add(eventService);
				(Stats as OperationStats)?.Propagate();
			}

			return enhancedService;
		}

		private TServiceInterface GetEnhancedService()
		{
			var enhancedService = factory.CreateEnhancedServiceInternal(false);
			CreatedServices++;
			return enhancedService;
		}

		public void ClearFactoryCache()
		{
			factory.ClearCache();
		}

		public void ReleaseService(IEnhancedOrgService enhancedService)
		{
			if (enhancedService is EnhancedOrgServiceBase enhancedOrgServiceBase)
			{
				var releasedServices = enhancedOrgServiceBase.ClearConnectionQueue();

				foreach (var service in releasedServices)
				{
					crmServicesQueue.Enqueue(service);
				}
			}

			if (enhancedService is TServiceInterface thisService)
			{
				servicesQueue.Enqueue(thisService);
			}
		}
	}
}
