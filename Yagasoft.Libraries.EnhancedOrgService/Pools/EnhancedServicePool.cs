#region Imports

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Microsoft.Xrm.Client.Runtime.Serialization;
using Yagasoft.Libraries.Common;
using Yagasoft.Libraries.EnhancedOrgService.Factories;
using Yagasoft.Libraries.EnhancedOrgService.Services;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Tooling.Connector;
using Yagasoft.Libraries.EnhancedOrgService.Helpers;
using Yagasoft.Libraries.EnhancedOrgService.Params;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Pools
{
	/// <inheritdoc cref="IEnhancedServicePool{TService}"/>
	public class EnhancedServicePool<TService> : IEnhancedServicePool<TService>
		where TService : EnhancedOrgServiceBase
	{
		private readonly EnhancedServiceFactory<TService> factory;

		private readonly BlockingQueue<TService> servicesQueue = new BlockingQueue<TService>();
		private readonly ConcurrentQueue<IOrganizationService> crmServicesQueue = new ConcurrentQueue<IOrganizationService>();

		private readonly PoolParams poolParams;
		private int createdCrmServicesCount;

		private Thread warmupThread;
		private bool isWarmUp;

		public EnhancedServicePool(EnhancedServiceFactory<TService> factory, int poolSize = 2)
		{
			this.factory = factory;
			poolParams = new PoolParams { PoolSize = poolSize };
		}

		public EnhancedServicePool(EnhancedServiceFactory<TService> factory, PoolParams poolParams = null)
		{
			this.factory = factory;
			this.poolParams = poolParams ?? new PoolParams();
		}

		public int CreatedServices { get; private set; }
		public int CurrentPoolSize => servicesQueue.Count;

		public TService GetService(int threads = 1)
		{
			servicesQueue.TryTake(out var service);
			return GetInitialisedService(threads, service);
		}

		public void WarmUp()
		{
			lock (this)
			{
				isWarmUp = true;

				if (warmupThread?.IsAlive == true)
				{
					return;
				}

				warmupThread =
					new Thread(
						() =>
						{
							while (isWarmUp && createdCrmServicesCount < poolParams.PoolSize)
							{
								try
								{
									lock (crmServicesQueue)
									{
										crmServicesQueue.Enqueue(GetCrmService(true));
									}
								}
								catch
								{
									// ignored
								}
							}
						}) { IsBackground = true };

				warmupThread.Start();
			}
		}

		public void EndWarmup()
		{
			isWarmUp = false;
		}

		private IOrganizationService GetCrmService(bool isSkipQueue = false)
		{
			lock (crmServicesQueue)
			{
				IOrganizationService crmService = null;

				if (!isSkipQueue)
				{
					crmServicesQueue.TryDequeue(out crmService);
				}

				if (crmService.EnsureTokenValid(poolParams.TokenExpiryCheckSecs) == false)
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
		}
		
	    private TService GetInitialisedService(int threads, TService enhancedService = null, bool isWarmup = false)
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
			    enhancedService = enhancedService ?? servicesQueue.Dequeue(poolParams.DequeueTimeoutInMillis);
		    }
		    catch (TimeoutException)
		    {
			    lock (servicesQueue)
			    {
				    enhancedService = GetEnhancedService();
			    }
		    }
		    
			enhancedService.FillServicesQueue(Enumerable.Range(0, threads).Select(e => GetCrmService()));
		    enhancedService.ReleaseService = () => ReleaseService(enhancedService);

			return enhancedService;
		}

		private TService GetEnhancedService()
		{
			var enhancedService = factory.CreateEnhancedServiceInternal(false);
			CreatedServices++;
			return enhancedService;
		}

		public void ClearFactoryCache()
		{
			factory.ClearCache();
		}

		public void ReleaseService(TService enhancedService)
		{
			lock (crmServicesQueue)
			{
				var releasedServices = enhancedService.ClearServicesQueue();

				foreach (var service in releasedServices)
				{
					crmServicesQueue.Enqueue(service);
				}
			}

			servicesQueue.Enqueue(enhancedService);
		}
	}
}
