#region Imports

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Microsoft.Xrm.Sdk;
using Yagasoft.Libraries.Common;
using Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.Planning;
using Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.SdkMocks;
using Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.SerialiseWorkarounds;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.Base
{
	[DataContract]
	[KnownType(typeof(PlannedValue))]
	public abstract class PlannedMapBase : IPlannedValue
	{
		[DataMember]
		public Guid Id { get; internal set; }

		[DataMember]
		public Guid? ParentId { get; internal set; }

		[DataMember]
		public string Alias { get; internal set; }

		[DataMember]
		internal readonly MockDictionary InnerDictionary = new MockDictionary();

		protected PlannedMapBase(Guid? parentId, string alias)
		{
			Id = Guid.NewGuid();
			ParentId = parentId;
			Alias = alias;
		}

		/// <summary>
		///     Gets a respective representation of one of the SDK types, to be used in place of real SDK types in future calls.
		///     <br />
		///     E.g. GetValue<![CDATA[<PlannedOptionSetValue>]]>, will return a <see cref="PlannedOptionSetValue" /> to be used in
		///     place of an <see cref="OptionSetValue" /> in a future <see cref="Entity" />
		///     <see cref="IOrganizationService.Create" /> or something similar.<br />
		///     E.g. GetValue<![CDATA[<PlannedEntityReference>]]>, will return a <see cref="PlannedEntityReference" />
		///     to be used in place of an <see cref="EntityReference" /> in a future <see cref="Entity" />
		///     <see cref="IOrganizationService.Update" /> or something similar.<br />
		///     For primitive or .NET values, call GetValue<![CDATA[<PlannedValue>]]> and use it as is in future calls.
		///     Those values will be replaced upon execution from where this value came.
		/// </summary>
		/// <typeparam name="TPlannedValue">Returned <see cref="IPlannedValue" /> type.</typeparam>
		/// <param name="name">Key to access the value during execution.</param>
		/// <exception cref="NotSupportedException">Given type must be a class that inherit from {nameof(IPlannedValue)}.</exception>
		public TPlannedValue GetPlannedValue<TPlannedValue>(string name) where TPlannedValue : IPlannedValue
		{
			var existingValue = InnerDictionary[name];

			if (existingValue is TPlannedValue typedValue)
			{
				return typedValue;
			}

			if (!typeof(TPlannedValue).IsClass)
			{
				throw new NotSupportedException($"Given type must be a class that inherit from {nameof(IPlannedValue)}.");
			}

			var plannedValue = (TPlannedValue)Activator.CreateInstance(typeof(TPlannedValue), BindingFlags.NonPublic | BindingFlags.Instance,
				null, new object[] { Id, name }, null);
			InnerDictionary[name] = plannedValue;

			return plannedValue;
		}

		/// <summary>
		/// <see cref="PlannedValue"/>-specific convenience for <see cref="GetPlannedValue{TPlannedValue}"/>.
		/// </summary>
		public PlannedValue this[string name] => GetPlannedValue<PlannedValue>(name);
	}
}
