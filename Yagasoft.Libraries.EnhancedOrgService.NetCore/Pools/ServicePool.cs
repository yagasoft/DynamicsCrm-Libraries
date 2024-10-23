#region Imports

using System;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.PowerPlatform.Dataverse.Client.Utils;
using Microsoft.Xrm.Sdk;
using Yagasoft.Libraries.Common;
using Yagasoft.Libraries.EnhancedOrgService.Factories;
using Yagasoft.Libraries.EnhancedOrgService.Helpers;
using Yagasoft.Libraries.EnhancedOrgService.Params;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Pools
{
	public class ServicePool<TService> : IServicePool<TService>
		where TService : IOrganizationService
	{
		public virtual int CreatedServices
		{
			get => CreatedServicesCount;
			protected set => CreatedServicesCount = value;
		}

		public virtual int CurrentPoolSize => ServicesQueue.Count;
		
		public virtual bool IsAutoPoolSize
		{
			get => isAutoPoolSize;
			set => isAutoPoolSize = value;
		}
		

		public virtual int MaxPoolSize
		{
			get => maxPoolSize;
			set
			{
				maxPoolSize = value;
				
				if (PoolParams.IsMaxPerformance)
				{
					var maxThreads = MaxPoolSize + 5;

					if (minThreadPool is null)
					{
						ThreadPool.GetMinThreads(out var minThreadPoolThreads, out var completionPortThreads);
						minThreadPool = Math.Max(minThreadPoolThreads, completionPortThreads);
					}

					ThreadPool.SetMinThreads((minThreadPool ?? 1) + maxThreads, (minThreadPool ?? 1) + maxThreads);
				}
			}
		}

		public virtual IServiceFactory<TService> Factory => factory;

		internal readonly PoolParams PoolParams;

		protected readonly BlockingQueue<TService> ServicesQueue = new();
		protected readonly BlockingQueue<object> PreemptiveServicesQueue = new();

		protected int CreatedServicesCount;

		private readonly IServiceFactory<TService> factory;

		private bool isAutoPoolSize;
		private int maxPoolSize;
		private int? minThreadPool;

		public ServicePool(IServiceFactory<TService> factory, int poolSize = -1)
			: this(factory, new PoolParams { PoolSize = poolSize })
		{ }

		public ServicePool(IServiceFactory<TService> factory, PoolParams? poolParams = null)
		{
			factory.Require(nameof(factory));

			this.factory = factory;

			if (poolParams != null)
			{
				poolParams.IsLocked = true;
			}

			PoolParams = poolParams ?? new PoolParams();

			isAutoPoolSize = PoolParams.IsAutoPoolSize;
			maxPoolSize = PoolParams.PoolSize ?? int.MaxValue;

			ParamHelpers.SetPerformanceParams(PoolParams);

			new Thread(
				() =>
				{
					while (true)
					{
						try
						{
							var isCreate = false;

							PreemptiveServicesQueue.Dequeue();

							lock (ServicesQueue)
							{
								if (CreatedServices < MaxPoolSize && ServicesQueue.Count <= 5)
								{
									CreatedServicesCount++;
									isCreate = true;
								}
							}

							if (!isCreate)
							{
								continue;
							}
							
							var service = factory.CreateService();

							if (service is ServiceClient serviceClient)
							{
								serviceClient.DisableCrossThreadSafeties = true;
							}

							ServicesQueue.Enqueue(service);
						}
						catch (DataverseConnectionException)
						{
							AutoSizeDecrement();
							PreemptiveServicesQueue.Enqueue(this);
						}
						catch
						{
							// ignored
						}
					}
				}) { IsBackground = true }.Start();
		}

		public virtual void WarmUp()
		{
			foreach (var _ in Enumerable.Range(1, IsAutoPoolSize ? 2 : (PoolParams.PoolSize ?? 2)))
			{
				PreemptiveServicesQueue.Enqueue(this);
			}
		}

		public virtual void EndWarmup()
		{
			PreemptiveServicesQueue.Clear();
		}

		public virtual TService GetService()
		{
			ServicesQueue.TryTake(out var service);

			if (service?.EnsureTokenValid((int)(PoolParams.TokenExpiryCheck ?? TimeSpan.FromMinutes(5)).TotalSeconds) == false)
			{
				service = default;
			}

			if (service != null)
			{
				return service;
			}

			PreemptiveServicesQueue.Enqueue(this);

			try
			{
				service = ServicesQueue.Dequeue(PoolParams.DequeueTimeout);
			}
			catch (TimeoutException)
			{
				service = factory.CreateService();

				lock (ServicesQueue)
				{
					CreatedServicesCount++;
				}
			}

			return service;
		}

		public virtual void ReleaseService(IOrganizationService service)
		{
			service.Require(nameof(service));

			if (service is TService thisService)
			{
				lock (ServicesQueue)
				{
					if (CreatedServices > MaxPoolSize)
					{
						CreatedServicesCount--;
						return;
					}
				}
				
				ServicesQueue.Enqueue(thisService);
			}
			else
			{
				throw new NotSupportedException("Given service is not supported by this pool.");
			}
		}

		public void AutoSizeIncrement()
		{
			if (IsAutoPoolSize && MaxPoolSize < PoolParams.PoolSize)
			{
				MaxPoolSize += 5;
			}
		}

		public void AutoSizeDecrement()
		{
			IsAutoPoolSize = false;
			MaxPoolSize = Math.Max(Math.Min(MaxPoolSize, CreatedServices) - 5, 2);
		}

		protected TService GetNewService()
		{
			var service = factory.CreateService();
			CreatedServicesCount++;
			return service;
		}
	}
}
