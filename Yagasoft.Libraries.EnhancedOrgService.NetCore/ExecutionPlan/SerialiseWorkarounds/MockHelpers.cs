using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Yagasoft.Libraries.Common;
using Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.Planning;

namespace Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.SerialiseWorkarounds
{
	internal static class MockHelpers
	{
		internal static T Mock<T>(this object item)
		{
			if (item is OrganizationRequest request)
			{
				return (T)(object)
					new MockOrgRequest
					{
						Name = request.RequestName,
						Parameters = new MockDictionary(request.Parameters?
							.Select(e => new MockKeyValue(e.Key, e.Value.Mock<object>())))
					};
			}

			if (item is OrganizationResponse response)
			{
				return (T)(object)
					new MockOrgResponse
					{
						Name = response.ResponseName,
						Results = new MockDictionary(response.Results?
							.Select(e => new MockKeyValue(e.Key, e.Value.Mock<object>())))
					};
			}

			if (item is Entity entity)
			{
				return (T)(object)
					new MockEntity
					{
						Id = entity.Id,
						LogicalName = entity.LogicalName,
						Attributes = new MockDictionary(entity.Attributes?
							.Select(e => new MockKeyValue(e.Key, e.Value.Mock<object>()))),
						Keys = new MockDictionary(entity.KeyAttributes?
							.Select(e => new MockKeyValue(e.Key, e.Value.Mock<object>())))
					};
			}

			if (item is EntityReference entityReference)
			{
				return (T)(object)
					new MockEntityReference
					{
						Id = entityReference.Id,
						LogicalName = entityReference.LogicalName,
						Keys = new MockDictionary(entityReference.KeyAttributes?
							.Select(e => new MockKeyValue(e.Key, e.Value.Mock<object>())))
					};
			}

			if (item is EntityCollection entityCollection)
			{
				return (T)(object)
					new MockEntityCollection
					{
						Collection = entityCollection.Entities?.Select(e => e.Mock<MockEntity>()).ToArray()
					};
			}

			if (item is EntityReferenceCollection entityReferenceCollection)
			{
				return (T)(object)
					new MockEntityReferenceCollection
					{
						Collection = entityReferenceCollection.ToArray()
					};
			}

			if (item is ColumnSet columnSet)
			{
				return (T)(object)
					new MockColumnSet
					{
						AllColumns = columnSet.AllColumns,
						Columns = columnSet.Columns?.ToArray()
					};
			}

			if (item is OrganizationRequestCollection
				|| item is QueryExpression || item is QueryByAttribute
				|| !(item is IPlannedValue || item.GetType().IsAnsiClass || item.GetType().IsPrimitive || item.GetType().IsValueType))
			{
				throw new InvalidPluginExecutionException($"{item.GetType().Name} is not supported by planned execution.");
			}

			return (T)item;
		}

		internal static T Unmock<T>(this object item)
		{
			if (item is MockOrgRequest request)
			{
				var collection = new ParameterCollection();
				collection.AddRange(request.Parameters?
					.Select(e => new KeyValuePair<string, object>(e.Key, e.Value?.Unmock<object>()))
					?? new KeyValuePair<string, object>[0]);

				return (T)(object)
					new OrganizationRequest
					{
						RequestName = request.Name,
						Parameters = collection
					};
			}

			if (item is MockOrgResponse response)
			{
				var collection = new ParameterCollection();
				collection.AddRange(response.Results?
					.Select(e => new KeyValuePair<string, object>(e.Key, e.Value?.Unmock<object>()))
					?? new KeyValuePair<string, object>[0]);

				return (T)(object)
					new OrganizationResponse
					{
						ResponseName = response.Name,
						Results = collection
					};
			}

			if (item is MockEntity entity)
			{
				var attributes = new AttributeCollection();
				attributes.AddRange(entity.Attributes?
					.Select(e => new KeyValuePair<string, object>(e.Key, e.Value?.Unmock<object>()))
					?? new KeyValuePair<string, object>[0]);
				var keys = new KeyAttributeCollection();
				keys.AddRange(entity.Keys?
					.Select(e => new KeyValuePair<string, object>(e.Key, e.Value?.Unmock<object>()))
					?? new KeyValuePair<string, object>[0]);

				return (T)(object)
					new Entity(entity.LogicalName, entity.Id)
					{
						Attributes = attributes,
						KeyAttributes = keys
					};
			}
			
			if (item is MockEntityReference entityReference)
			{
				var keys = new KeyAttributeCollection();
				keys.AddRange(entityReference.Keys?
					.Select(e => new KeyValuePair<string, object>(e.Key, e.Value?.Unmock<object>()))
					?? new KeyValuePair<string, object>[0]);

				return (T)(object)
					new EntityReference(entityReference.LogicalName, entityReference.Id)
					{
						KeyAttributes = keys
					};
			}

			if (item is MockEntityCollection entityCollection)
			{
				return (T)(object)new EntityCollection(entityCollection.Collection?.Select(e => e?.Unmock<Entity>()).ToList());
			}

			if (item is MockEntityReferenceCollection entityReferenceCollection)
			{
				return (T)(object)new EntityReferenceCollection(entityReferenceCollection.Collection?
					.Select(e => e?.Unmock<EntityReference>()).ToList());
			}

			if (item is MockColumnSet columnSet)
			{
				return (T)(object)(columnSet.Columns == null
					? new ColumnSet(columnSet.AllColumns)
					: new ColumnSet(columnSet.Columns) { AllColumns = columnSet.AllColumns });
			}

			return (T)item;
		}
	}
}
