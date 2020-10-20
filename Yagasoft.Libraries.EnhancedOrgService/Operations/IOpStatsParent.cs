using System.Collections.Generic;

namespace Yagasoft.Libraries.EnhancedOrgService.Response.Operations
{
    public interface IOpStatsParent 
    {
	    IEnumerable<IOpStatsParent> Containers { get; }
	    IEnumerable<IOperationStats> StatTargets { get; }
    }
}
