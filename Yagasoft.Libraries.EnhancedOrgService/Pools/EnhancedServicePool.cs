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

		private readonly int poolSize;
		private readonly int tokenExpiryCheckSecs;
		private int createdServicesCount;

		public EnhancedServicePool(EnhancedServiceFactory<TService> factory, int poolSize = 10,
		    int tokenExpiryCheckSecs = 600)
		{
			this.factory = factory;
			this.poolSize = poolSize;
		    this.tokenExpiryCheckSecs = tokenExpiryCheckSecs;
		}

		public TService GetService(int threads = 1)
		{
			servicesQueue.TryTake(out var service);
			return GetInitialisedService(threads, service);
		}

		private IOrganizationService GetCrmService()
		{
			crmServicesQueue.TryDequeue(out var crmService);

		    if (ConnectionHelpers.EnsureTokenValid(crmService, tokenExpiryCheckSecs) == false)
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
					if (createdServicesCount < poolSize)
					{
						enhancedService = factory.CreateEnhancedServiceInternal(false);
						createdServicesCount++;
					}
				}
			}

			enhancedService = enhancedService ?? servicesQueue.Dequeue();
			enhancedService.ReleaseService = () => ReleaseService(enhancedService);
			enhancedService.FillServicesQueue(Enumerable.Range(0, threads).Select(e => GetCrmService()));
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
