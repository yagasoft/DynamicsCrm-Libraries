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

				if (AutoRetryParams != null)
				{
					AutoRetryParams.IsLocked = value;
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
			get => connectionParams ??= new ConnectionParams();
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
			get => cachingParams;
			set
			{
				ValidateLock();
				cachingParams = value;
				IsCachingEnabled = value != null;
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
			get => transactionParams;
			set
			{
				ValidateLock();
				transactionParams = value;
				IsTransactionsEnabled = value != null;
			}
		}

		public bool IsAutoRetryEnabled
		{
			get => isAutoRetryEnabled;
			set
			{
				ValidateLock();

				if (isAutoRetryEnabled && AutoRetryParams == null)
				{
					throw new ArgumentNullException(nameof(AutoRetryParams),
						"Must set concurrency parameters first before enabling concurrency.");
				}

				isAutoRetryEnabled = value;
			}
		}

		public AutoRetryParams AutoRetryParams
		{
			get => autoRetryParams ??= new AutoRetryParams();
			set
			{
				ValidateLock();
				value.Require(nameof(AutoRetryParams));
				autoRetryParams = value;
				IsAutoRetryEnabled = true;
			}
		}

		public PoolParams PoolParams
		{
			get => poolParams;
			set
			{
				ValidateLock();
				poolParams = value;
			}
		}

		private bool isLocked;

		private ConnectionParams connectionParams;

		private bool isCachingEnabled;
		private CachingParams cachingParams;

		private bool isTransactionsEnabled;
		private TransactionParams transactionParams;

		private bool isAutoRetryEnabled;
		private AutoRetryParams autoRetryParams;

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
