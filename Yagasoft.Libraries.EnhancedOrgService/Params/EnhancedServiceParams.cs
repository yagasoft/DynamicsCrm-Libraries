using System;

namespace Yagasoft.Libraries.EnhancedOrgService.Params
{
	public class EnhancedServiceParams : ParamsBase
	{
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
				poolParams = value;
			}
        }
	}
}
