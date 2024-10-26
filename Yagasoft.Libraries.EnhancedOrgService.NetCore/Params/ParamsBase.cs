#region Imports

using System;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Params
{
	/// <summary>
	///     Defines some parameters or settings for services.<br />
	///     Parameters will be locked upon use by a service.
	/// </summary>
	public abstract class ParamsBase
	{
		public bool IsLocked
		{
			get => isLocked;
			set
			{
				foreach (var property in GetType().GetProperties()
					.Where(p => p.PropertyType.IsAssignableTo(typeof(ParamsBase)))
					.Select(p => p.GetValue(this))
					.OfType<ParamsBase>())
				{
					property.IsLocked = value;
				}
				
				isLocked = value;
			}
		}

		private bool isLocked;
		
		public void ValidateLock()
		{
			if (IsLocked)
			{
				throw new NotSupportedException("Cannot modify value because object is locked.");
			}
		}
	}
}
