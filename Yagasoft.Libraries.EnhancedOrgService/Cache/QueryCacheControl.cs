#region Imports

using System.Collections.Generic;
using Yagasoft.Libraries.Common;
using Microsoft.Xrm.Sdk.Query;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Cache
{
	internal class QueryCacheControl
	{
		private readonly Dictionary<string, string> queryCache = new Dictionary<string, string>();

		public void AddCachedQuery(QueryBase query, QueryExpression queryExpression)
		{
			var serialisedQuery = SerialiserHelpers.SerialiseXml(query);
			queryCache[serialisedQuery] = SerialiserHelpers.SerialiseXml(queryExpression);
		}

		public QueryExpression GetCachedQuery(QueryBase query)
		{
			var serialisedQuery = SerialiserHelpers.SerialiseXml(query);
			queryCache.TryGetValue(serialisedQuery, out var queryExpression);
			return queryExpression == null ? null : SerialiserHelpers.DeserializeXml<QueryExpression>(queryExpression);
		}

		public void ClearCache()
		{
			queryCache.Clear();
		}
	}
}
