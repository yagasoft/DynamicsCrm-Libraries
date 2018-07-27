using System.Diagnostics;
using System.Linq;
using Microsoft.FxCop.Sdk;

namespace LinkDev.Libraries.DynamicsCrmRules
{
	internal sealed class EnforcePluginNoDepthCheck : BaseFxCopRule
	{
		public override TargetVisibilities TargetVisibility => TargetVisibilities.All;

		public EnforcePluginNoDepthCheck() : base("EnforcePluginNoDepthCheck")
		{

		}

		public override ProblemCollection Check(Member member)
		{
			var method = member as Method;
			;
			if (method?.DeclaringType == null
				|| !IsContainsPluginOrActivity(method.ContainingAssembly()) || !IsUserCode(method.DeclaringType))
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

		public override void VisitMethodCall(MethodCall call)
		{
			if (call != null)
			{
				if (IsDepthBinding(call))
				{
					AddProblem(call);
				}
			}

			base.VisitMethodCall(call);
		}

		private bool IsDepthBinding(MethodCall call)
		{
			if (!(call.Callee is MemberBinding binding))
			{
				return false;
			}

			return (binding.BoundMember as Method)?.DeclaringMember?.FullName?
				.Contains("Microsoft.Xrm.Sdk.IExecutionContext.Depth") == true;
		}
	}
}
