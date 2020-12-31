#region Imports

using System;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Yagasoft.Libraries.EnhancedOrgService.Cache;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Helpers
{
	internal static class RequestHelper
	{
		private static readonly QueryCacheControl queryCache = new();

		internal static int GetTotalRecordsCount<TQuery>(IOrganizationService service, TQuery query)
			where TQuery : QueryBase
		{
			var queryTemp = query as QueryExpression;

			var pageInfoBackup = queryTemp?.PageInfo;
			var columnsBackup = queryTemp?.ColumnSet;
			queryTemp = queryTemp == null ? CloneQuery(service, query) : CloneQueryRefsOnly(queryTemp);

			var totalRecordCount = 0;

			EntityCollection queryResult;

			queryTemp.PageInfo =
				new PagingInfo
				{
					PageNumber = 1
				};

			queryTemp.ColumnSet = new ColumnSet(false);

			do
			{
				queryResult = service.RetrieveMultiple(queryTemp);
				totalRecordCount += queryResult.Entities.Count;
				queryTemp.PageInfo.PagingCookie = queryResult.PagingCookie;
				queryTemp.PageInfo.PageNumber++;
			}
			while (queryResult.MoreRecords);

			queryTemp.PageInfo = pageInfoBackup;
			queryTemp.ColumnSet = columnsBackup;

			return totalRecordCount;
		}

		internal static int GetTotalPagesCount(IOrganizationService service, QueryBase query, int pageSize = 5000,
			int? totalRecordsCount = null)
		{
			return (int)Math.Ceiling((totalRecordsCount
				?? GetTotalRecordsCount(service, query)) / (double)pageSize);
		}

		internal static string GetCookie<TQuery>(IOrganizationService service, TQuery query,
			int? pageSize = null, int? page = null)
			where TQuery : QueryBase
		{
			var queryTemp = query as QueryExpression;

			var pageInfoBackup = queryTemp?.PageInfo;
			var columnsBackup = queryTemp?.ColumnSet;
			queryTemp = queryTemp == null ? CloneQuery(service, query) : CloneQueryRefsOnly(queryTemp);

			EntityCollection queryResult;

			queryTemp.PageInfo =
				new PagingInfo
				{
					PageNumber = 1,
					Count = pageSize ?? 5000
				};

			queryTemp.ColumnSet = new ColumnSet(false);

			string cookie;

			do
			{
				queryResult = service.RetrieveMultiple(queryTemp);
				cookie = queryTemp.PageInfo.PagingCookie = queryResult.PagingCookie;
				queryTemp.PageInfo.PageNumber++;
			}
			while (queryResult.MoreRecords && queryTemp.PageInfo.PageNumber < page);

			queryTemp.PageInfo = pageInfoBackup;
			queryTemp.ColumnSet = columnsBackup;

			return cookie;
		}

		internal static QueryExpression CloneQuery(IOrganizationService service, QueryBase query,
			string fetchXml = null)
		{
			var cachedQuery = queryCache.GetCachedQuery(query);

			if (cachedQuery != null)
			{
				return cachedQuery;
			}

			// get the FetchXML to create new queries for each page
			fetchXml ??= ((QueryExpressionToFetchXmlResponse)
				service.Execute(
					new QueryExpressionToFetchXmlRequest
					{
						Query = query
					})).FetchXml;

			// create a new QueryExpression object
			cachedQuery = ((FetchXmlToQueryExpressionResponse)
				service.Execute(
					new FetchXmlToQueryExpressionRequest
					{
						FetchXml = fetchXml
					})).Query;

			queryCache.AddCachedQuery(query, cachedQuery);

			return cachedQuery;
		}

		internal static QueryExpression CloneQueryRefsOnly(QueryExpression query)
		{
			var newQuery =
				new QueryExpression(query.EntityName)
				{
					ColumnSet = query.ColumnSet,
					Criteria = query.Criteria,
					Distinct = query.Distinct,
					NoLock = query.NoLock,
					TopCount = query.TopCount,
					PageInfo = query.PageInfo == null
						? null
						: new PagingInfo
						  {
							  Count = query.PageInfo.Count,
							  PageNumber = query.PageInfo.PageNumber,
							  PagingCookie = query.PageInfo.PagingCookie,
							  ReturnTotalRecordCount = query.PageInfo.ReturnTotalRecordCount
						  }
				};

			newQuery.LinkEntities.AddRange(query.LinkEntities);
			newQuery.Orders.AddRange(query.Orders);

			return newQuery;
		}
	}
}
