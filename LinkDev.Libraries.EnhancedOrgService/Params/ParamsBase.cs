using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinkDev.Libraries.EnhancedOrgService.Exceptions;

namespace LinkDev.Libraries.EnhancedOrgService.Params
{
    public abstract class ParamsBase
    {
		public bool IsLocked { get; internal set; }

		public void ValidateLock()
		{
			if (IsLocked)
			{
				throw new UnsupportedException("Cannot modify value because object is locked.");
			}
		}
	}
}
