#region Imports

using System;
using Yagasoft.Libraries.EnhancedOrgService.Exceptions;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Params
{
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
