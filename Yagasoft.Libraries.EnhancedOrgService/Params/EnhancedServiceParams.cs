#region Imports

using Yagasoft.Libraries.Common;
using Yagasoft.Libraries.EnhancedOrgService.Operations;
using Yagasoft.Libraries.EnhancedOrgService.Response.Operations;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Params
{
	public class EnhancedServiceParams : EnhancedServiceParamsBase
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

		public AutoRetryParams AutoRetryParams
		{
			get => autoRetryParams;
			set
			{
				ValidateLock();
				autoRetryParams = value;
				IsAutoRetryEnabled = value != null;
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

		/// <summary>
		///     Sets a limit on the number of operations to keep in <see cref="IOperationStats.ExecutedOperations" /><br />
		///     Default: <see cref="int.MaxValue" />.
		/// </summary>
		public int? OperationHistoryLimit
		{
			get => operationHistoryLimit;
			set
			{
				ValidateLock();
				value?.RequireAtLeast(0, nameof(OperationHistoryLimit));
				operationHistoryLimit = value;
			}
		}

		private bool isLocked;
		private ConnectionParams connectionParams;
		private CachingParams cachingParams;
		private TransactionParams transactionParams;
		private AutoRetryParams autoRetryParams;
		private PoolParams poolParams;
		private int? operationHistoryLimit;

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
			ConnectionParams ??= new ConnectionParams();
			ConnectionParams.DotNetDefaultConnectionLimit = 30000;
			ConnectionParams.IsDotNetDisableWaitForConnectConfirm = true;
			ConnectionParams.IsDotNetDisableNagleAlgorithm = true;

			PoolParams ??= new PoolParams();
			PoolParams.DotNetSetMinAppReservedThreads = 100;
		}
	}
}
