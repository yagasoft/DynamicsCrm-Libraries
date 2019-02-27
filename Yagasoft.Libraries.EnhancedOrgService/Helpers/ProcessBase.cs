using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yagasoft.Libraries.EnhancedOrgService.Builders;
using Yagasoft.Libraries.EnhancedOrgService.Exceptions;

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
