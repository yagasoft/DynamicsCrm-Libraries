namespace Yagasoft.Libraries.EnhancedOrgService.Params
{
	public class AutoRetryParams : ParamsBase
	{
		public bool IsAsyncAppHold
		{
			get => isAsyncAppHold;
			set
			{
				ValidateLock();
				isAsyncAppHold = value;
			}
		}

		private bool isAsyncAppHold;
	}
}
