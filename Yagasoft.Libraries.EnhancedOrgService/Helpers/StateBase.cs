#region Imports

using Yagasoft.Libraries.EnhancedOrgService.Exceptions;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Helpers
{
	public abstract class StateBase
	{
		protected bool IsValid;

		internal void ValidateState(bool isValid = true)
		{
			if (isValid && !IsValid)
			{
				throw new StateException("Object is in an invalid state.");
			}

			if (!isValid && IsValid)
			{
				throw new StateException("Object is in a valid state.");
			}
		}
	}
}
