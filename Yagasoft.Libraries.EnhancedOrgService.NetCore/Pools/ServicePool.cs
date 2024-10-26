#region Imports

using System;
using System.Collections.Concurrent;
using System.ServiceModel;
using System.Xml.Linq;

using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.PowerPlatform.Dataverse.Client.Utils;
using Microsoft.Rest;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json.Linq;

using Yagasoft.Libraries.Common;
using Yagasoft.Libraries.EnhancedOrgService.Events;
using Yagasoft.Libraries.EnhancedOrgService.Events.EventArgs;
using Yagasoft.Libraries.EnhancedOrgService.Exceptions;
using Yagasoft.Libraries.EnhancedOrgService.Factories;
using Yagasoft.Libraries.EnhancedOrgService.Helpers;
using Yagasoft.Libraries.EnhancedOrgService.Params;
using Yagasoft.Libraries.EnhancedOrgService.Response.Operations;
using Yagasoft.Libraries.EnhancedOrgService.Services.SelfDisposing;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Pools
{
	public class ServicePool<TService> : IServicePool<TService>
		where TService : IOrganizationService
	{
		public virtual int CurrentPoolSize
		{
			get => currentPoolSize;
			protected set => currentPoolSize = value;
		}

		public virtual bool IsAutoPoolSize { get; set; }

		public virtual int MaxPoolSize
		{
			get => maxPoolSize;
			set
			{
				maxPoolSemaphore.WaitAsync().Wait();

				var currentMaxPoolSize = maxPoolSize;
				
				try
				{
					maxPoolSize = value;
					consumeSemaphore.MaxConcurrency = maxPoolSize;
					CurrentPoolSize += maxPoolSize - currentMaxPoolSize;
				}
				finally
				{
					maxPoolSemaphore.Release();
				}
			}
		}

		public virtual IServiceFactory<TService> Factory => factory;

		public PoolParams PoolParams { get; }

		public virtual EventHandler<IOrganizationService, OperationStatusEventArgs, IOrganizationService>? OperationsEventHandler { get; set; }

		public int? RecommendedDegreesOfParallelism => (Service as ServiceClient)?.RecommendedDegreesOfParallelism;
		
		protected SelfDisposingService? SelfDisposingService;
		protected TService? Service;
		protected TService? BackupService;

		private readonly IServiceFactory<TService> factory;

		private int currentPoolSize;
		private int maxPoolSize;
		
		private readonly SemaphoreSlim initSemaphore = new(1);
		private readonly FifoSemaphore consumeSemaphore = new(1);
		private readonly SemaphoreSlim maxPoolSemaphore = new(1);
		private readonly SemaphoreSlim throttleSemaphore = new(1);
		private int semaphoreExtra;
		
		private TimeSpan requestsDuration = TimeSpan.Zero;
		private readonly SemaphoreSlim durationSemaphore = new(1);
		
		private ConcurrentDictionary<long, long> requests = new();
		private readonly ConcurrentDictionary<long, long> throttledRequests = new();
		private long requestId;

		public ServicePool(IServiceFactory<TService> factory, int poolSize = -1)
			: this(factory, new PoolParams { PoolSize = poolSize })
		{ }

		public ServicePool(IServiceFactory<TService> factory, PoolParams poolParams)
		{
			factory.Require(nameof(factory));
			poolParams.Require(nameof(poolParams));
			
			this.factory = factory;

			poolParams.IsLocked = true;
			PoolParams = poolParams;
			
			OperationsEventHandler += OnOperationStatusChanged;
		}

		public virtual async Task<TService> GetService()
		{
			if (Service == null)
			{
				await initSemaphore.WaitAsync();
				
				if (Service == null)
				{
					var serviceTask = SwitchService();

					IsAutoPoolSize = PoolParams.IsAutoPoolSize;
					MaxPoolSize = PoolParams.PoolSize ?? 2;

					ParamHelpers.SetPerformanceParams(PoolParams);

					if (PoolParams.IsMaxPerformance)
					{
						ThreadPool.GetMinThreads(out var minThreadPoolThreads, out var completionPortThreads);
						var currentMinThreads = Math.Max(minThreadPoolThreads, completionPortThreads);
						var minThreads = Math.Max(Math.Max(MaxPoolSize, 100) + currentMinThreads, currentMinThreads);
						ThreadPool.SetMinThreads(minThreads, minThreads);
					}
				
					await serviceTask;
					
					if (Service is ServiceClient serviceClient && IsAutoPoolSize)
					{
						MaxPoolSize = serviceClient.RecommendedDegreesOfParallelism;
					}
				}
				
				initSemaphore.Release();
			}

			try
			{
				if (await consumeSemaphore.WaitAsync(PoolParams.DequeueTimeout ?? TimeSpan.FromMinutes(7)))
				{
					Interlocked.Decrement(ref currentPoolSize);
				}
				else
				{
					Interlocked.Increment(ref semaphoreExtra);
				}
			}
			catch
			{
				// ignored
			}
			
			return Service;
		}

		public virtual async Task<IDisposableService> GetSelfDisposingService()
		{
			var service = await GetService();
			
			if (service == null)
			{
				throw new StateException("Failed to find an internal CRM service.");
			}

			return SelfDisposingService ??=
				new SelfDisposingService(service, () => ReleaseService(service), this as IServicePool<IOrganizationService>);
		}

		public virtual void ReleaseService(IOrganizationService service)
		{
			if (semaphoreExtra > 0)
			{
				Interlocked.Decrement(ref semaphoreExtra);
				return;
			}

			consumeSemaphore.Release();
			Interlocked.Increment(ref currentPoolSize);
		}

		protected async Task<TService> GetNewService()
		{
			return await factory.CreateService();
		}

		private async Task SwitchService()
		{
			Service = BackupService ?? await factory.CreateService();

			if (SelfDisposingService is not null)
			{
				SelfDisposingService.Service = Service;
			}
			_ = Task.Run(async () => BackupService = await factory.CreateService());
		}
		
		protected virtual void OnOperationStatusChanged(IOrganizationService sender, OperationStatusEventArgs e, IOrganizationService s)
		{
			switch (e.Status)
			{
				case Status.Ready:
					break;
				
				case Status.InProgress:
					Task.Run(RegisterRequest);
					break;
				
				case Status.RequestDone:
					Task.Run(async
						() =>
						{
							_ = RecordDuration(e.Operation.RequestDuration);

							await maxPoolSemaphore.WaitAsync();

							try
							{
								if (IsAutoPoolSize && Service is ServiceClient serviceClient)
								{
									var recommended = serviceClient.RecommendedDegreesOfParallelism;

									if (recommended != MaxPoolSize)
									{
										MaxPoolSize = recommended;
									}
								}
							}
							finally
							{
								maxPoolSemaphore.Release();
							}
						});
					break;
				
				case Status.Retry:
					HandleFailure(e.Operation, e.Operation.PreException);
					break;
				
				case Status.Success:
					break;
				
				case Status.Failure:
					HandleFailure(e.Operation, e.Operation.Exception);
					break;
			}
		}

		private async Task RecordDuration(TimeSpan? duration)
		{
			if (duration is null)
			{
				return;
			}

			await durationSemaphore.WaitAsync();

			try
			{
				requestsDuration = requestsDuration.Add(duration.Value);

				if (requestsDuration > TimeSpan.FromMinutes(20))
				{
					_ = Task.Run(SwitchService);
					requestsDuration = TimeSpan.Zero;
				}
			}
			finally
			{
				durationSemaphore.Release();
			}
		}

		private async Task RegisterRequest()
		{
			var id = Interlocked.Increment(ref requestId);
			requests[id] = id;

			await Task.Delay(TimeSpan.FromMinutes(5));

			await throttleSemaphore.WaitAsync();

			try
			{
				if (requests.TryRemove(id, out _))
				{
					return;
				}

				if (throttledRequests.TryRemove(id, out _))
				{
					if (throttledRequests.Count <= 0)
					{
						consumeSemaphore.IsBlocked = false;
						consumeSemaphore.ReleaseAllBlocked();
					}
					else
					{
						consumeSemaphore.ReleaseBlocked();
					}
				}
			}
			finally
			{
				throttleSemaphore.Release();
			}
		}

		private void HandleFailure(Operation operation, Exception? exception)
		{
			if (exception is not FaultException<OrganizationServiceFault> svcFault
				|| Factory.ServiceParams.ConnectionParams?.IsApiLimit(svcFault.Detail.ErrorCode) is not true)
			{
				return;
			}
			
			throttleSemaphore.WaitAsync().Wait();

			try
			{
				consumeSemaphore.IsBlocked = true;

				var newThrottledRequests = requests;
				requests = new ConcurrentDictionary<long, long>();

				foreach (var (key, value) in newThrottledRequests)
				{
					throttledRequests[key] = value;
				}
			}
			finally
			{
				throttleSemaphore.Release();
			}
		}
	}
}
