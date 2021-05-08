namespace Yagasoft.Libraries.EnhancedOrgService.Params
{
	public class ServiceParamsBase : ParamsBase
	{
		public bool? IsCachingEnabled
		{
			get => isCachingEnabled;
			set
			{
				ValidateLock();
				isCachingEnabled = value;
			}
		}

		public bool? IsTransactionsEnabled
		{
			get => isTransactionsEnabled;
			set
			{
				ValidateLock();
				isTransactionsEnabled = value;
			}
		}

		/// <summary>
		///     Enables auto retry on operations.
		///     Must use the 'enhanced version' of the operations to get a status report.
		/// </summary>
		public bool? IsAutoRetryEnabled
		{
			get => isAutoRetryEnabled;
			set
			{
				ValidateLock();
				isAutoRetryEnabled = value;
			}
		}

		private bool? isCachingEnabled;
		private bool? isTransactionsEnabled;
		private bool? isAutoRetryEnabled;
	}
}
