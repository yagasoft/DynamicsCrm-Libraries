#region Imports

using System;
using System.Runtime.Serialization;
using Microsoft.Xrm.Sdk;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.Planning
{
	[DataContract]
	public class PlannedValue : IPlannedValue
	{
		[DataMember]
		public Guid Id { get; set; }

		[DataMember]
		public Guid? ParentId { get; set; }

		[DataMember]
		public string Alias { get; set; }

		public PlannedValue(Guid? parentId, string alias)
		{
			Id = Guid.NewGuid();
			ParentId = parentId;
			Alias = alias;
		}

		/// <summary>
		///     <seealso cref="GetInnerValue{T}" />
		/// </summary>
		public Guid GetInnerValue()
		{
			return Id;
		}

		/// <summary>
		///     Gets a respective representation of the planned value to be used in objects like <see cref="OptionSetValue" />
		///     (<see cref="int" /> value), <see cref="EntityReference" /> (<see cref="string" /> for logical name, or
		///     <see cref="Guid" /> for ID).<br />
		///     Those values will be replaced upon execution from where this <see cref="PlannedValue" /> came from.
		/// </summary>
		/// <typeparam name="T">Return type.</typeparam>
		public T GetInnerValue<T>()
		{
			var str = Id.ToString().ToLower();

			return
				typeof(T).Name switch {
					nameof(String) => (T)(object)str,
					nameof(Int32) => (T)(object)str.GetHashCode(),
					_ => throw new NotImplementedException()
					};
		}
	}
}
