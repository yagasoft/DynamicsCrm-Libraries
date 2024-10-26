#region Imports

using Yagasoft.Libraries.Common;
using Yagasoft.Libraries.EnhancedOrgService.Operations;
using Yagasoft.Libraries.EnhancedOrgService.Response.Operations;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Params
{
	public class SelfBalancingParams : ParamsBase
	{
		public ConnectionParams ConnectionParams
		{
			get => connectionParams;
			set
			{
				ValidateLock();
				value.Require(nameof(ConnectionParams));
				connectionParams = value;
			}
		}

		public PoolParams PoolParams
		{
			get => poolParams;
			set
			{
				ValidateLock();
				poolParams = value;
			}
		}

		private bool isLocked;
		private ConnectionParams connectionParams;
		private PoolParams poolParams;

		public SelfBalancingParams(string connectionString, PoolParams poolParams)
		{
			ConnectionParams =
				new ConnectionParams
				{
					ConnectionString = connectionString
				};
			PoolParams = poolParams;
		}

		public SelfBalancingParams(ConnectionParams connectionParams, PoolParams poolParams)
		{
			ConnectionParams = connectionParams;
			PoolParams = poolParams;
		}

		/// <summary>
		///     Automatically sets all performance parameters in this object tree to try to achieve the best performance possible.
		///     <br />
		///     Those parameters are app-wide -- global on the .Net Framework level; so they will affect all logic in this process.
		/// </summary>
		public SelfBalancingParams AutoSetMaxPerformanceParams()
		{
			ConnectionParams.DotNetDefaultConnectionLimit = 65000;
			ConnectionParams.IsDotNetDisableWaitForConnectConfirm = true;
			ConnectionParams.IsDotNetDisableNagleAlgorithm = true;
			ConnectionParams.IsMaxPerformance = true;

			PoolParams.IsMaxPerformance = true;
			
			return this;
		}
	}
}
