#region Imports

using System;
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

		public virtual IServiceFactory<TService> Factory => factory;

		protected readonly PoolParams PoolParams;

		protected readonly BlockingQueue<TService> ServicesQueue = new();

		protected int CreatedServicesCount;

		private readonly IServiceFactory<TService> factory;

		private WarmUp.WarmUp warmUp;

		public ServicePool(IServiceFactory<TService> factory, int poolSize = 2)
			: this(factory, new PoolParams { PoolSize = poolSize })
		{ }

		public ServicePool(IServiceFactory<TService> factory, PoolParams poolParams = null)
		{
			factory.Require(nameof(factory));

			this.factory = factory;

			if (poolParams != null)
			{
				poolParams.IsLocked = true;
			}

			PoolParams = poolParams ?? new PoolParams();

			ParamHelpers.SetPerformanceParams(poolParams);
		}

		public virtual void WarmUp()
		{
			lock (this)
			{
				warmUp ??= new WarmUp.WarmUp(
					() =>
					{
						if (CreatedServicesCount < PoolParams.PoolSize)
						{
							ServicesQueue.Enqueue(GetNewService());
						}
						else
						{
							EndWarmup();
						}
					});
			}

			warmUp.Start();
		}

		public virtual void EndWarmup()
		{
			warmUp.End();
		}

		public virtual TService GetService()
		{
			ServicesQueue.TryTake(out var service);

			if (service.EnsureTokenValid((int)(PoolParams.TokenExpiryCheck ?? TimeSpan.FromMinutes(5)).TotalSeconds) == false)
			{
				service = default;
			}

			if (service != null)
			{
				return service;
			}

			service = factory.CreateService();
			CreatedServicesCount++;

			return service;
		}

		public virtual void ReleaseService(IOrganizationService service)
		{
			service.Require(nameof(service));

			if (service is TService thisService)
			{
				ServicesQueue.Enqueue(thisService);
			}
			else
			{
				throw new NotSupportedException("Given service is not supported by this pool.");
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
