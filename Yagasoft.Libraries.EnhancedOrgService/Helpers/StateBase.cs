using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yagasoft.Libraries.EnhancedOrgService.Builders;
using Yagasoft.Libraries.EnhancedOrgService.Exceptions;

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
			else if (!isValid && IsValid)
			{
				throw new StateException("Object is in n valid state.");
			}
		}
	}
}
