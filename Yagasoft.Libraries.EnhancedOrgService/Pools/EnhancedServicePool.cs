#region Imports

using System;
using System.Collections.Concurrent;
using System.Linq;
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

		private int createdServicesCount;

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

		public TService GetService(int threads = 1)
		{
			servicesQueue.TryTake(out var service);
			return GetInitialisedService(threads, service);
		}

		private IOrganizationService GetCrmService()
		{
			crmServicesQueue.TryDequeue(out var crmService);

		    if (ConnectionHelpers.EnsureTokenValid(crmService, poolParams.TokenExpiryCheckSecs) == false)
		    {
		        crmService = null;
		    }

		    return crmService ?? factory.CreateCrmService();
		}
		
	    private TService GetInitialisedService(int threads, TService enhancedService = null)
		{
			if (enhancedService == null)
			{
				lock (servicesQueue)
				{
					if (createdServicesCount < poolParams.PoolSize)
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
				enhancedService = GetEnhancedService();
			}

			enhancedService.ReleaseService = () => ReleaseService(enhancedService);
			enhancedService.FillServicesQueue(Enumerable.Range(0, threads).Select(e => GetCrmService()));
			return enhancedService;
		}

		private TService GetEnhancedService()
		{
			var enhancedService = factory.CreateEnhancedServiceInternal(false);
			createdServicesCount++;
			return enhancedService;
		}

		public void ReleaseService(TService enhancedService)
		{
			var releasedServices = enhancedService.ClearServicesQueue();

			foreach (var service in releasedServices)
			{
				crmServicesQueue.Enqueue(service);
			}

			servicesQueue.Enqueue(enhancedService);
		}
	}
}
