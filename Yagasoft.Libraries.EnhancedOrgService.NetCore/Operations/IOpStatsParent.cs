using System.Collections.Generic;

namespace Yagasoft.Libraries.EnhancedOrgService.Operations
{
    public interface IOpStatsParent 
    {
	    IEnumerable<IOperationStats> StatTargets { get; }
    }
}
