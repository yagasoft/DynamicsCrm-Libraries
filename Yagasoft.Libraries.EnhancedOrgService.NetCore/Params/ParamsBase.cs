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
		public virtual bool IsLocked { get; internal set; }

		public void ValidateLock()
		{
			if (IsLocked)
			{
				throw new NotSupportedException("Cannot modify value because object is locked.");
			}
		}
	}
}
