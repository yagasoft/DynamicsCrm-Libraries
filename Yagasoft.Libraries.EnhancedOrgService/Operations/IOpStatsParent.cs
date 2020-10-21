using System.Collections.Generic;

namespace Yagasoft.Libraries.EnhancedOrgService.Operations
{
    public interface IOpStatsParent 
    {
	    IEnumerable<IOpStatsParent> Containers { get; }
	    IEnumerable<IOperationStats> StatTargets { get; }
    }
}
