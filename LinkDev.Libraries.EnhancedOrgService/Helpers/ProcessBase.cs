using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinkDev.Libraries.EnhancedOrgService.Builders;
using LinkDev.Libraries.EnhancedOrgService.Exceptions;

namespace LinkDev.Libraries.EnhancedOrgService.Helpers
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
			else if (!isInitialised && IsInitialised)
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
			else if (!isFinalised && IsFinalised)
			{
				throw new FinalisationException("Process cannot be finalised.");
			}
		}
	}
}
