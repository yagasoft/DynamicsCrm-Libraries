#region Imports

using System;
using Microsoft.Xrm.Sdk;
using Yagasoft.Libraries.EnhancedOrgService.Factories;
using Yagasoft.Libraries.EnhancedOrgService.Params;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Pools
{
	/// <summary>
	///     A straightforward service pool of vanilla CrmServiceClient connections.
	/// </summary>
	public class DefaultServicePool : ServicePool<IOrganizationService>
	{
		public DefaultServicePool(string connectionString, int poolSize = -1, TimeSpan? tokenExpiryCheck = null)
			: this(new ConnectionParams { ConnectionString = connectionString }, poolSize, tokenExpiryCheck)
		{ }

		public DefaultServicePool(ConnectionParams connectionParams, int poolSize = -1, TimeSpan? tokenExpiryCheck = null)
			: this(connectionParams, new PoolParams { PoolSize = poolSize, TokenExpiryCheck = tokenExpiryCheck })
		{ }

		public DefaultServicePool(string connectionString, PoolParams poolParams = null)
			: this(new ConnectionParams { ConnectionString = connectionString }, poolParams)
		{ }

		public DefaultServicePool(ServiceParams serviceParams) : this(serviceParams.ConnectionParams, serviceParams.PoolParams)
		{ }

		public DefaultServicePool(ConnectionParams connectionParams, PoolParams poolParams = null)
			: base(new ServiceFactory(connectionParams, poolParams?.TokenExpiryCheck), poolParams)
		{ }

		public DefaultServicePool(IServiceFactory<IOrganizationService> factory, int poolSize = -1, TimeSpan? tokenExpiryCheck = null)
			: this(factory, new PoolParams { PoolSize = poolSize, TokenExpiryCheck = tokenExpiryCheck })
		{ }

		public DefaultServicePool(IServiceFactory<IOrganizationService> factory, PoolParams poolParams = null)
			: base(factory, poolParams)
		{ }
	}
}
