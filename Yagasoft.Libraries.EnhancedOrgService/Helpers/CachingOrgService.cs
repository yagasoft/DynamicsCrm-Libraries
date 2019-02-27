using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Client;
using Microsoft.Xrm.Client.Caching;
using Microsoft.Xrm.Client.Services;

namespace LinkDev.Libraries.EnhancedOrgService.Helpers
{
	public class CachingOrgService
	{
		public string Id { get; set; }
		public CrmConnection Connection { get; private set; }
		public ObjectCache Cache { get; private set; }
		public IOrganizationServiceCache ServiceCache { get; private set; }
		public CachedOrganizationService CachedOrganizationService { get; private set; }

		private int invalidationInterval;
		private DateTime latestInvalidationDate;

		public CachingOrgService(string connectionString, int invalidationInterval = 20)
		{
			var connection = CrmConnection.Parse(connectionString);

			if (((WhoAmIResponse)new OrganizationService(connection).Execute(new WhoAmIRequest())).UserId == Guid.Empty)
			{
				throw new Exception("Can't create connection to: \"" + connectionString + "\".");
			}

			Init(connection, connectionString, invalidationInterval);
		}

		public CachingOrgService(CrmConnection connection, string id, int invalidationInterval = 20)
		{
			Init(connection, id, invalidationInterval);
		}

		private void Init(CrmConnection connection, string id, int invalidationInterval = 20)
		{
			Id = id;
			Connection = connection;
			Cache = new MemoryCache(id);
			ServiceCache = new OrganizationServiceCache(Cache, Connection);
			CachedOrganizationService = new CachedOrganizationService(Connection, ServiceCache);
			this.invalidationInterval = invalidationInterval;
			latestInvalidationDate = DateTime.Now;
		}

		public void CheckClearInterval()
		{
			lock (this)
			{
				if ((DateTime.Now - latestInvalidationDate) < TimeSpan.FromMinutes(invalidationInterval))
				{
					return;
				}

				ClearCache();
				latestInvalidationDate = DateTime.Now;
			}
		}

		public void ClearCache()
		{
			Cache.Clear();
		}
	}
}
