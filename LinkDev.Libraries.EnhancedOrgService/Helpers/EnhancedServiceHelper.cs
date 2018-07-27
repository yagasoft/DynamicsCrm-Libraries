#region Imports

using LinkDev.Libraries.EnhancedOrgService.Builders;
using LinkDev.Libraries.EnhancedOrgService.Factories;
using LinkDev.Libraries.EnhancedOrgService.Params;
using LinkDev.Libraries.EnhancedOrgService.Pools;
using LinkDev.Libraries.EnhancedOrgService.Services;

#endregion

namespace LinkDev.Libraries.EnhancedOrgService.Helpers
{
	/// <summary>
	///     Provides methods to quickly, and easily create Enhanced Org Services.<br />
	///     Author: Ahmed el-Sawalhy
	/// </summary>
	public static class EnhancedServiceHelper
	{
		public static EnhancedServicePool<Services.EnhancedOrgService> GetPool(string connectionString, int poolSize = 10,
			EnhancedServiceParams serviceParams = null)
		{
			var builder = EnhancedServiceBuilder.NewBuilder.Initialise(connectionString);

			if (serviceParams?.IsCachingEnabled == true)
			{
				builder.AddCaching(serviceParams.CachingParams);
			}

			if (serviceParams?.IsConcurrencyEnabled == true)
			{
				builder.AddConcurrency(serviceParams.ConcurrencyParams);
			}

			if (serviceParams?.IsTransactionsEnabled == true)
			{
				builder.AddTransactions(serviceParams.TransactionParams);
			}

			var build = builder.Finalise().GetBuild();
			var factory = new EnhancedServiceFactory<Services.EnhancedOrgService>(build);
			return new EnhancedServicePool<Services.EnhancedOrgService>(factory, poolSize);
		}

		public static EnhancedServicePool<AsyncOrgService> GetAsyncPool(string connectionString, int poolSize = 10,
			EnhancedServiceParams serviceParams = null, bool isHoldAppForAsync = true)
		{
			var builder = EnhancedServiceBuilder.NewBuilder.Initialise(connectionString);

			if (serviceParams?.IsCachingEnabled == true)
			{
				builder.AddCaching(serviceParams.CachingParams);
			}

			if (serviceParams?.IsConcurrencyEnabled != false)
			{
				builder.AddConcurrency(serviceParams?.ConcurrencyParams);

				if (isHoldAppForAsync)
				{
					builder.HoldAppForAsync();
				}
			}

			if (serviceParams?.IsTransactionsEnabled == true)
			{
				builder.AddTransactions(serviceParams.TransactionParams);
			}

			var build = builder.Finalise().GetBuild();
			var factory = new EnhancedServiceFactory<AsyncOrgService>(build);
			return new EnhancedServicePool<AsyncOrgService>(factory, poolSize);
		}
	}
}
