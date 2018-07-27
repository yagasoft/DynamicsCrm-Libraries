#region Imports

using System.Collections.Concurrent;
using System.Linq;
using LinkDev.Libraries.Common;
using LinkDev.Libraries.EnhancedOrgService.Factories;
using LinkDev.Libraries.EnhancedOrgService.Services;
using Microsoft.Xrm.Sdk;

#endregion

namespace LinkDev.Libraries.EnhancedOrgService.Pools
{
	/// <inheritdoc cref="IEnhancedServicePool{TService}"/>
	public class EnhancedServicePool<TService> : IEnhancedServicePool<TService>
		where TService : EnhancedOrgServiceBase
	{
		private readonly EnhancedServiceFactory<TService> factory;

		private readonly BlockingQueue<TService> servicesQueue = new BlockingQueue<TService>();
		private readonly ConcurrentQueue<IOrganizationService> crmServicesQueue = new ConcurrentQueue<IOrganizationService>();

		private readonly int poolSize;
		private int createdServicesCount;

		public EnhancedServicePool(EnhancedServiceFactory<TService> factory, int poolSize = 10)
		{
			this.factory = factory;
			this.poolSize = poolSize;
		}

		public TService GetService(int threads = 1)
		{
			servicesQueue.TryTake(out var service);
			return GetInitialisedService(threads, service);
		}

		private IOrganizationService GetCrmService()
		{
			crmServicesQueue.TryDequeue(out var crmService);
			return crmService ?? factory.CreateCrmService();
		}

		private TService GetInitialisedService(int threads,
			TService enhancedService = null)
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
