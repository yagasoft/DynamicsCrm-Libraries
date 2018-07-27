using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.FxCop.Sdk;

namespace LinkDev.Libraries.DynamicsCrmRules
{
	internal sealed class EnforceSvcCreate : BaseFxCopRule
	{
		public override TargetVisibilities TargetVisibility => TargetVisibilities.All;
		private static int crmOrgSvcInitCount;
		
		public EnforceSvcCreate() : base("EnforceSvcCreate")
		{

		}

		public override ProblemCollection Check(Member member)
		{
			var method = member as Method;

			if (method?.DeclaringType == null || !IsUserCode(method.DeclaringType))
			{
				// This rule only applies to certain nodes.
				// Return a null ProblemCollection so no violations are reported for this member.
				return null;
			}

			Visit(method);

			// By default the Problems collection is empty so no violations will be reported
			// unless a check found and added a problem.
			return Problems;
		}

		public override void VisitConstruct(Construct construct)
		{
			if (construct != null)
			{
				if (construct.Type.FullName.Contains("Microsoft.Xrm.Tooling.Connector.CrmServiceClient"))
				{
					if (++crmOrgSvcInitCount > 1)
					{
						AddProblem(construct, crmOrgSvcInitCount);
					}
				}
			}

			base.VisitConstruct(construct);
		}
	}
}
