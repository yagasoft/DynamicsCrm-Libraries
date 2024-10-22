using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Client.Caching;
using Microsoft.Xrm.Client.Services.Messages;
using Microsoft.Xrm.Sdk;
using Yagasoft.Libraries.EnhancedOrgService.Params;

namespace Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced.Cache
{
    public class CachingOrgService : EnhancedOrgService, ICachingOrgService
    {
	    protected internal CachingOrgService(ServiceParams parameters) : base(parameters)
	    { }

	    public virtual void RemoveFromCache(Entity record)
	    {
		    Cache?.Remove(record);
	    }

	    public virtual void RemoveFromCache(EntityReference entity)
	    {
		    Cache?.Remove(entity);
	    }

	    public virtual void RemoveFromCache(string entityLogicalName, Guid? id)
	    {
		    Cache?.Remove(entityLogicalName, id);
	    }

	    public virtual void RemoveFromCache(OrganizationRequest request)
	    {
		    Cache?.Remove(request);
	    }

	    public virtual void RemoveAllFromCache()
	    {
		    Cache?.Remove(new OrganizationServiceCachePluginMessage { Category = CacheItemCategory.All });
	    }

	    /// <inheritdoc />
	    public virtual void ClearCache()
	    {
		    if (Parameters.CachingParams?.CacheScope != CacheScope.Service)
		    {
			    throw new NotSupportedException("The memory cache is not limited to this service."
				    + " Use the factory's method to clear the cache instead.");
		    }

		    ObjectCache?.Clear();
	    }
    }
}
