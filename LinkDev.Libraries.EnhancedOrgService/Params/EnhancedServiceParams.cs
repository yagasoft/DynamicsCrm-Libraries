namespace LinkDev.Libraries.EnhancedOrgService.Params
{
	public class EnhancedServiceParams : ParamsBase
	{
		private string connectionString;

		private bool isCachingEnabled;
		private CachingParams cachingParams;

		private bool isTransactionsEnabled;
		private TransactionParams transactionParams;

		private bool isConcurrencyEnabled;
		private ConcurrencyParams concurrencyParams;

		public string ConnectionString
		{
			get => connectionString;
			set
			{
				ValidateLock();
				connectionString = value;
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
			}
		}

		public bool IsConcurrencyEnabled
		{
			get => isConcurrencyEnabled;
			set
			{
				ValidateLock();
				isConcurrencyEnabled = value;
			}
		}

		public ConcurrencyParams ConcurrencyParams
		{
			get => concurrencyParams;
			set
			{
				ValidateLock();
				concurrencyParams = value;
			}
		}
	}
}
