namespace Yagasoft.Libraries.EnhancedOrgService.Params
{
    public class ConcurrencyParams : ParamsBase
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
