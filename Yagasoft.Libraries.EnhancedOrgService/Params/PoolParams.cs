namespace Yagasoft.Libraries.EnhancedOrgService.Params
{
    public class PoolParams : ParamsBase
    {
	    private int? poolSize;

		/// <summary>
		/// Default value: 2 connections.
		/// </summary>
	    public int PoolSize
	    {
		    get => poolSize ?? 2;
		    set
		    {
			    ValidateLock();
			    poolSize = value;
		    }
	    }

	    private int? tokenExpiryCheckSecs;

		/// <summary>
		/// Threshold time in seconds from actual expiry of the connection token.
		/// Can be used in service pools to automatically renew tokens.<br />
		/// Default value: 1 hour.
		/// </summary>
	    public int TokenExpiryCheckSecs
	    {
		    get => tokenExpiryCheckSecs ?? 60 * 60;
		    set
		    {
			    ValidateLock();
			    tokenExpiryCheckSecs = value;
		    }
	    }

	    private int? dequeueTimeoutInMillis;

		/// <summary>
		/// Threshold time in seconds from actual expiry of the connection token.
		/// Can be used in service pools to automatically renew tokens.<br />
		/// Default value: 2 minutes.
		/// </summary>
	    public int DequeueTimeoutInMillis
	    {
		    get => dequeueTimeoutInMillis ?? 2 * 60 * 1000;
		    set
		    {
			    ValidateLock();
			    dequeueTimeoutInMillis = value;
		    }
	    }
    }
}
