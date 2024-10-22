#region Imports

using Yagasoft.Libraries.EnhancedOrgService.Exceptions;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Helpers
{
	public abstract class ProcessBase
	{
		protected bool IsInitialised;
		protected bool IsFinalised;

		internal void ValidateInitialised(bool isInitialised = true)
		{
			if (isInitialised && !IsInitialised)
			{
				throw new InitialisationException("Process must be initialised.");
			}

			if (!isInitialised && IsInitialised)
			{
				throw new InitialisationException("Process cannot be initialised.");
			}
		}

		internal void ValidateFinalised(bool isFinalised = true)
		{
			if (isFinalised && !IsFinalised)
			{
				throw new FinalisationException("Process must be finalised.");
			}

			if (!isFinalised && IsFinalised)
			{
				throw new FinalisationException("Process cannot be finalised.");
			}
		}
	}
}
