#region Imports

using System;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.PowerPlatform.Dataverse.Client.Utils;
using Microsoft.Xrm.Sdk;
using Yagasoft.Libraries.Common;
using Yagasoft.Libraries.EnhancedOrgService.Factories;
using Yagasoft.Libraries.EnhancedOrgService.Helpers;
using Yagasoft.Libraries.EnhancedOrgService.NetCore.Helpers;
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

		public virtual int CurrentPoolSize => Services.Count;
		
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

		protected readonly List<TService> Services = new();
		private int currentService = 0;
		private int busyServices = 0;

		protected int CreatedServicesCount;

		private readonly IServiceFactory<TService> factory;

		private bool isAutoPoolSize;
		private int maxPoolSize;
		private int? minThreadPool;
		
		private readonly SemaphoreSlim initSemaphore = new(0);
		private readonly SemaphoreSlim createSemaphore = new(20);

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

			IsAutoPoolSize = PoolParams.IsAutoPoolSize;
			MaxPoolSize = PoolParams.PoolSize ?? int.MaxValue;

			ParamHelpers.SetPerformanceParams(PoolParams);
		}

		public virtual void WarmUp()
		{
			foreach (var _ in Enumerable.Range(1, IsAutoPoolSize ? 2 : (PoolParams.PoolSize ?? 2)))
			{
				
			}
		}

		public virtual void EndWarmup()
		{
			
		}

		public virtual async Task<TService> GetService()
		{
			TService? service = default;

			var isCreate = false;

			lock (Services)
			{
				if (busyServices >= CreatedServices && CreatedServices < MaxPoolSize)
				{
					CreatedServicesCount++;
					isCreate = true;
				}
			}

			if (isCreate)
			{
				while (service == null)
				{
					try
					{
						try
						{
							await createSemaphore.WaitAsync();
							service = factory.CreateService();
						}
						finally
						{
							createSemaphore.Release();
						}

						Services.Add(service);
						initSemaphore.Release();
					}
					catch (DataverseConnectionException)
					{
						AutoSizeDecrement();

						if (MaxPoolSize <= 2)
						{
							break;
						}
					}
					catch
					{
						// ignored
					}
				}
			}

			if (Services.Count <= 0)
			{
				await initSemaphore.WaitAsync();
			}
			
			lock (Services)
			{
				service = Services[currentService];

				currentService++;

				if (currentService >= Services.Count)
				{
					currentService = 0;
				}

				busyServices++;
			}

			return service;
		}

		public virtual void ReleaseService(IOrganizationService service)
		{
			service.Require(nameof(service));
			busyServices--;
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

			if (Services.Count > 5)
			{
				lock (Services)
				{
					Services.RemoveRange(Services.Count - 6, 5);
				}
			}
		}

		protected TService GetNewService()
		{
			var service = factory.CreateService();
			CreatedServicesCount++;
			return service;
		}
	}
}
