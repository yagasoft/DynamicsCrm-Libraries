using System.Diagnostics;
using System.Linq;
using Microsoft.FxCop.Sdk;

namespace LinkDev.Libraries.DynamicsCrmRules
{
	internal sealed class EnforceCrmHandleGuid : BaseFxCopRule
	{
		public override TargetVisibilities TargetVisibility => TargetVisibilities.All;

		public EnforceCrmHandleGuid() : base("EnforceCrmHandleGuid")
		{

		}

		public override ProblemCollection Check(Member member)
		{
			var method = member as Method;
			;
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

		public override void VisitAddressDereference(AddressDereference dereference)
		{
			if (dereference?.Address is UnaryExpression expression)
			{
				if ((expression.Type as Reference)?.ConstructorName.Name.Contains("Guid@") == true)
				{
					AddProblem(expression);
				}
			}

			base.VisitAddressDereference(dereference);
		}

		public override void VisitMethodCall(MethodCall call)
		{
			if (call != null)
			{
				if ((call.Callee as MemberBinding)?.BoundMember.FullName.Contains("System.Guid.NewGuid") == true)
				{
					AddProblem(call);
				}
			}

			base.VisitMethodCall(call);
		}
	}
}
