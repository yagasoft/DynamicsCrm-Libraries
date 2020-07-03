namespace Yagasoft.Libraries.EnhancedOrgService.Params
{
    public class ConnectionParams : ParamsBase
    {
	    private string connectionString;

	    public string ConnectionString
	    {
		    get => connectionString;
		    set
		    {
			    ValidateLock();
			    connectionString = value;
		    }
	    }
    }
}
