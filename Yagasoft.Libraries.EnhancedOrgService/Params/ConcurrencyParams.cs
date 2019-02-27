namespace Yagasoft.Libraries.EnhancedOrgService.Params
{
    public class ConcurrencyParams : ParamsBase
    {
	    private bool isAsyncAppHold;

	    public bool IsAsyncAppHold
	    {
		    get => isAsyncAppHold;
		    set
		    {
			    ValidateLock();
			    isAsyncAppHold = value;
		    }
	    }
    }
}
