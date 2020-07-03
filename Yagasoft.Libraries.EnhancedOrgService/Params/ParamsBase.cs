using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yagasoft.Libraries.EnhancedOrgService.Exceptions;

namespace Yagasoft.Libraries.EnhancedOrgService.Params
{
    public abstract class ParamsBase
    {
		public virtual bool IsLocked { get; internal set; }

		public void ValidateLock()
		{
			if (IsLocked)
			{
				throw new UnsupportedException("Cannot modify value because object is locked.");
			}
		}
	}
}
