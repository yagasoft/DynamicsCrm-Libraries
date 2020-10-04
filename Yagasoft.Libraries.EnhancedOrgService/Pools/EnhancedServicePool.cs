﻿#region Imports

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Xrm.Sdk;
using Yagasoft.Libraries.Common;
using Yagasoft.Libraries.EnhancedOrgService.Factories;
using Yagasoft.Libraries.EnhancedOrgService.Helpers;
using Yagasoft.Libraries.EnhancedOrgService.Params;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced.Transactions;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Pools
{
	/// <inheritdoc cref="IEnhancedServicePool{TService}" />
	public class EnhancedServicePool<TServiceInterface, TEnhancedOrgService> : IEnhancedServicePool<TServiceInterface>
		where TServiceInterface : ITransactionOrgService
		where TEnhancedOrgService : EnhancedOrgServiceBase, TServiceInterface
	{
		private readonly EnhancedServiceFactory<TServiceInterface, TEnhancedOrgService> factory;

		private readonly BlockingQueue<TServiceInterface> servicesQueue = new BlockingQueue<TServiceInterface>();
		private readonly ConcurrentQueue<IOrganizationService> crmServicesQueue = new ConcurrentQueue<IOrganizationService>();

		private readonly PoolParams poolParams;
		private int createdCrmServicesCount;

		private Thread warmupThread;
		private bool isWarmUp;

		public EnhancedServicePool(EnhancedServiceFactory<TServiceInterface, TEnhancedOrgService> factory, int poolSize = 2)
		{
			this.factory = factory;
			poolParams = new PoolParams { PoolSize = poolSize };
		}

		public EnhancedServicePool(EnhancedServiceFactory<TServiceInterface, TEnhancedOrgService> factory, PoolParams poolParams = null)
		{
			this.factory = factory;
			this.poolParams = poolParams ?? new PoolParams();
		}

		public int CreatedServices { get; private set; }
		public int CurrentPoolSize => servicesQueue.Count;

		public TServiceInterface GetService(int threads = 1)
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
				enhancedService ??= servicesQueue.Dequeue(poolParams.DequeueTimeoutInMillis ?? 10);
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
					enhancedOrgServiceBase.FillServicesQueue(Enumerable.Range(0, threads).Select(e => GetCrmService()));
				}
				catch
				{
					enhancedOrgServiceBase.ReleaseService();
					throw;
				}
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

		public void ReleaseService(ITransactionOrgService enhancedService)
		{
			lock (crmServicesQueue)
			{
				if (enhancedService is EnhancedOrgServiceBase enhancedOrgServiceBase)
				{
					var releasedServices = enhancedOrgServiceBase.ClearServicesQueue();

					foreach (var service in releasedServices)
					{
						crmServicesQueue.Enqueue(service);
					}
				}
			}

			if (enhancedService is TServiceInterface thisService)
			{
				servicesQueue.Enqueue(thisService);
			}
		}
	}
}
