#region Imports

using System;
using Yagasoft.Libraries.Common;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Params
{
	public class EnhancedServiceParams : ParamsBase
	{
		public override bool IsLocked
		{
			get => isLocked;
			internal set
			{
				if (ConnectionParams != null)
				{
					ConnectionParams.IsLocked = value;
				}

				if (CachingParams != null)
				{
					CachingParams.IsLocked = value;
				}

				if (TransactionParams != null)
				{
					TransactionParams.IsLocked = value;
				}

				if (ConcurrencyParams != null)
				{
					ConcurrencyParams.IsLocked = value;
				}

				if (PoolParams != null)
				{
					PoolParams.IsLocked = value;
				}

				isLocked = value;
			}
		}

		public ConnectionParams ConnectionParams
		{
			get => connectionParams = connectionParams ?? new ConnectionParams();
			set
			{
				ValidateLock();
				value.Require(nameof(ConnectionParams));
				connectionParams = value;
			}
		}

		public bool IsCachingEnabled
		{
			get => isCachingEnabled;
			set
			{
				ValidateLock();
				isCachingEnabled = value;
			}
		}

		public CachingParams CachingParams
		{
			get => cachingParams = cachingParams ?? new CachingParams();
			set
			{
				ValidateLock();
				value.Require(nameof(CachingParams));
				cachingParams = value;
				IsCachingEnabled = true;
			}
		}

		public bool IsTransactionsEnabled
		{
			get => isTransactionsEnabled;
			set
			{
				ValidateLock();
				isTransactionsEnabled = value;
			}
		}

		public TransactionParams TransactionParams
		{
			get => transactionParams = transactionParams ?? new TransactionParams();
			set
			{
				ValidateLock();
				value.Require(nameof(TransactionParams));
				transactionParams = value;
				IsTransactionsEnabled = true;
			}
		}

		public bool IsConcurrencyEnabled
		{
			get => isConcurrencyEnabled;
			set
			{
				ValidateLock();

				if (isConcurrencyEnabled && ConcurrencyParams == null)
				{
					throw new ArgumentNullException(nameof(ConcurrencyParams),
						"Must set concurrency parameters first before enabling concurrency.");
				}

				isConcurrencyEnabled = value;
			}
		}

		public ConcurrencyParams ConcurrencyParams
		{
			get => concurrencyParams = concurrencyParams ?? new ConcurrencyParams();
			set
			{
				ValidateLock();
				value.Require(nameof(ConcurrencyParams));
				concurrencyParams = value;
				IsConcurrencyEnabled = true;
			}
		}

		public PoolParams PoolParams
		{
			get => poolParams = poolParams ?? new PoolParams();
			set
			{
				ValidateLock();
				value.Require(nameof(PoolParams));
				poolParams = value;
			}
		}

		private bool isLocked;

		private ConnectionParams connectionParams;

		private bool isCachingEnabled;
		private CachingParams cachingParams;

		private bool isTransactionsEnabled;
		private TransactionParams transactionParams;

		private bool isConcurrencyEnabled;
		private ConcurrencyParams concurrencyParams;

		private PoolParams poolParams;

		public EnhancedServiceParams()
		{ }

		public EnhancedServiceParams(string connectionString)
		{
			connectionParams = new ConnectionParams { ConnectionString = connectionString };
		}

		/// <summary>
		///     Automatically sets all performance parameters in this object tree to try to achieve the best performance possible.
		///     <br />
		///     Those parameters are app-wide -- global on the .Net Framework level; so they will affect all logic in this process.
		/// </summary>
		public void AutoSetMaxPerformanceParams()
		{
			if (ConnectionParams == null)
			{
				ConnectionParams = new ConnectionParams();
			}

			ConnectionParams.DotNetDefaultConnectionLimit = 30000;
			ConnectionParams.IsDotNetDisableWaitForConnectConfirm = true;
			ConnectionParams.IsDotNetDisableNagleAlgorithm = true;

			if (PoolParams == null)
			{
				PoolParams = new PoolParams();
			}

			PoolParams.DotNetSetMinAppReservedThreads = 100;
		}
	}
}
