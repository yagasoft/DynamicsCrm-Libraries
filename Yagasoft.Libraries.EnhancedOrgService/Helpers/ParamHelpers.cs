using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Yagasoft.Libraries.Common;
using Yagasoft.Libraries.EnhancedOrgService.Params;

namespace Yagasoft.Libraries.EnhancedOrgService.Helpers
{
    public static class ParamHelpers
    {
	    public static void SetPerformanceParams(ConnectionParams parameters)
	    {
		    parameters.Require(nameof(parameters));

		    if (parameters.DotNetDefaultConnectionLimit.HasValue)
		    {
			    ServicePointManager.DefaultConnectionLimit = parameters.DotNetDefaultConnectionLimit.Value;
		    }

		    if (parameters.IsDotNetDisableWaitForConnectConfirm.HasValue)
		    {
			    ServicePointManager.Expect100Continue =
				    !parameters.IsDotNetDisableWaitForConnectConfirm.Value;
		    }

		    if (parameters.IsDotNetDisableNagleAlgorithm.HasValue)
		    {
			    ServicePointManager.UseNagleAlgorithm = !parameters.IsDotNetDisableNagleAlgorithm.Value;
		    }
	    }

	    public static void SetPerformanceParams(PoolParams parameters)
	    {
		    parameters.Require(nameof(parameters));

		    if (parameters.DotNetSetMinAppReservedThreads.HasValue)
		    {
			    var minThreads = parameters.DotNetSetMinAppReservedThreads.Value;
			    ThreadPool.SetMinThreads(minThreads, minThreads);
		    }
	    }
    }
}
