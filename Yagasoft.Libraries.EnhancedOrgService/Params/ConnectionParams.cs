using System;
using Microsoft.Xrm.Sdk;

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

	    private Func<string, IOrganizationService> customIOrgSvcFactory;

		/// <summary>
		/// A custom factory that will be used to create CRM connections instead of the library built-in method.
		/// </summary>
		public Func<string, IOrganizationService> CustomIOrgSvcFactory
	    {
		    get => customIOrgSvcFactory;
		    set
		    {
			    ValidateLock();
			    customIOrgSvcFactory = value;
		    }
	    }
    }
}
