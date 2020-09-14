using System;
using System.Activities;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata.Query;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;

using Yagasoft.Libraries.Common;
using Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.Base;
using Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.Planning;
using Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.SerialiseWorkarounds;

namespace Yagasoft.Libraries.EnhancedOrgService
{
    public class Test
    {
	    private readonly IDictionary<Guid, object> valueMap = new Dictionary<Guid, object>();
	    private readonly IDictionary<Guid, OrganizationResponse> plannedValueMap = new Dictionary<Guid, OrganizationResponse>();

		public string ProcessOperations(string serialisedPlan, IOrganizationService service)
		{
			var assemblies = new[] { GetType().Assembly, typeof(Entity).Assembly };
			var operationsList = serialisedPlan.DeserialiseContractJson<List<PlannedOperation>>(true,
				null, assemblies, new DateTimeCrmContractSurrogateCustom());
			var operations = new Queue<PlannedOperation>(operationsList);

			while (operations.Any())
			{
				var operation = operations.Dequeue();
				var request = operation.Request.Unmock<OrganizationRequest>();
				ProcessValue(request);

				var response = service.Execute(request);

				var plannedResponse = operation.Response;
				valueMap[plannedResponse.Id] = response;
				ProcessValue(response, plannedResponse);
				plannedValueMap[plannedResponse.Id] = new OrganizationResponse { Results = response.Results };
			}

			return plannedValueMap.SerialiseContractJson(true, null, assemblies,
				new DateTimeCrmContractSurrogateCustom());
		}

		private object ProcessValue(object value, IPlannedValue plannedValueX = null)
		{
			if (plannedValueX != null)
			{
				valueMap[plannedValueX.Id] = value is AliasedValue aliased ? aliased.Value : value;
			}

			return value switch {
				OrganizationRequest e => ProcessMappedValue(e.Parameters, plannedValueX as PlannedMapBase, e),
				OrganizationResponse e => ProcessMappedValue(e.Results, plannedValueX as PlannedMapBase, e),
				Entity e => ProcessMappedValue(e.Attributes, plannedValueX as PlannedMapBase, e),
				EntityReference e => ProcessProperties(e, plannedValueX as PlannedMapBase),
				EntityCollection e => ProcessCollection(e.Entities, plannedValueX as PlannedMapBase, e),
				OptionSetValue e => ProcessProperties(e, plannedValueX as PlannedMapBase),
				DataCollection<string, object> e => ProcessMappedValue(e, plannedValueX as PlannedMapBase, e),
				DataCollection<Entity> e => ProcessCollection(e, plannedValueX as PlannedMapBase, e),
				DataCollection<EntityReference> e => ProcessCollection(e, plannedValueX as PlannedMapBase, e),
				DataCollection<OptionSetValue> e => ProcessCollection(e, plannedValueX as PlannedMapBase, e),
				DataCollection<object> e => ProcessCollection(e, plannedValueX as PlannedMapBase, e),
				IPlannedValue e => valueMap.TryGetValue(e.Id, out var mappedValue) ? mappedValue : e,
				_ => value != null && Guid.TryParse(value.ToString(), out var id)
					? (valueMap.TryGetValue(id, out var mappedId) ? mappedId : value) : value
				};
		}

		private object ProcessMappedValue(DataCollection<string, object> collection, PlannedMapBase planned, object obj)
		{
			// lookup values already saved and replace in object directly
			foreach (var pair in collection.ToDictionary(e => e.Key, e => e.Value))
			{
				collection[pair.Key] = ProcessValue(pair.Value);
			}

			if (planned == null)
			{
				ProcessProperties(obj);
				return obj;
			}
			
			// go over placeholders
			foreach (var mockKeyValue in planned.InnerDictionary.InnerList)
			{
				var key = mockKeyValue.Key;

				// does the placeholder exist in object?
				if (collection.TryGetValue(key, out var responseValue))
				{
					var plannedValue = mockKeyValue.Value as IPlannedValue;

					// lookup placeholder value already saved and replace in object
					if (plannedValue != null && valueMap.TryGetValue(plannedValue.Id, out var mappedValue))
					{
						collection[key] = mappedValue;
						continue;
					}

					// value not saved before, save it in the map and continue
					ProcessValue(responseValue, plannedValue);
				}
			}

			ProcessProperties(obj, planned);

			return obj;
		}

		private object ProcessCollection<T>(DataCollection<T> collection, PlannedMapBase planned, object obj)
		{
			// lookup values already saved and replace in object directly
			for (var i = 0; i < collection.Count; i++)
			{
				var processedValue = ProcessValue(collection[i]);
				collection[i] = processedValue is T value ? value : default;
			}

			if (planned == null)
			{
				return obj;
			}
			
			// go over placeholders
			foreach (var mockKeyValue in planned.InnerDictionary.InnerList)
			{
				var iStr = mockKeyValue.Key;

				// does the placeholder exist in object?
				if (int.TryParse(iStr, out var i) && i < collection.Count)
				{
					var plannedValue = mockKeyValue.Value as IPlannedValue;

					// lookup placeholder value already saved and replace in object
					if (plannedValue != null && valueMap.TryGetValue(plannedValue.Id, out var mappedValue))
					{
						collection[i] = mappedValue is T value ? value : default;
						continue;
					}

					// value not saved before, save it in the map and continue
					ProcessValue(collection[i], plannedValue);
				}
			}

			ProcessProperties(obj, planned);

			return obj;
		}

		private object ProcessProperties(object obj, PlannedMapBase planned = null)
		{
			var properties = obj.GetType().GetProperties();

			foreach (var property in properties)
			{
				try
				{
					var value = property.GetValue(obj);
					SavePropertyValue(property, value, ProcessValue(value), obj);
				}
				catch
				{
					// ignored
				}
			}

			if (planned == null)
			{
				return obj;
			}
			
			// go over placeholders
			foreach (var mockKeyValue in planned.InnerDictionary.InnerList)
			{
				var key = mockKeyValue.Key;
				var responseProperty = properties.FirstOrDefault(p => p.Name == key);

				// does the placeholder exist in object?
				if (responseProperty != null)
				{
					var plannedValue = mockKeyValue.Value as IPlannedValue;

					// lookup placeholder value already saved and replace in object
					if (plannedValue != null && valueMap.TryGetValue(plannedValue.Id, out var mappedValue))
					{
						try
						{
							SavePropertyValue(responseProperty, mappedValue, ProcessValue(mappedValue), obj);
						}
						catch
						{
							// ignored
						}

						continue;
					}

					object responseValue;

					try
					{
						responseValue = responseProperty.GetValue(obj);
					}
					catch
					{
						continue;
					}

					// value not saved before, save it in the map and continue
					ProcessValue(responseValue, plannedValue);
				}
			}

			return obj;
		}

	    private void SavePropertyValue(PropertyInfo property, object currentValue, object value, object obj)
	    {
		    if (currentValue?.Equals(value) == true)
		    {
			    return;
		    }

		    if (property.PropertyType.IsInstanceOfType(value))
		    {
			    property.SetValue(obj, value);
		    }

		    if (value != null && Guid.TryParse(value.ToString(), out var id))
		    {
				if (typeof(Guid?).IsAssignableFrom(property.PropertyType))
				{
					property.SetValue(obj, id);
				}
				else
				{
					property.SetValue(obj, id.ToString());
				}
		    }
	    }
    }
}
