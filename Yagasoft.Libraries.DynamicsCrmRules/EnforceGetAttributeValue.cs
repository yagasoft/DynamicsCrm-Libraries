using System.Diagnostics;
using System.Linq;
using Microsoft.FxCop.Sdk;

namespace Yagasoft.Libraries.DynamicsCrmRules
{
	internal sealed class EnforceGetAttributeValue : BaseFxCopRule
	{
		public override TargetVisibilities TargetVisibility => TargetVisibilities.All;

		public EnforceGetAttributeValue() : base("EnforceGetAttributeValue")
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

		public override void VisitMemberBinding(MemberBinding memberBinding)
		{
			if (memberBinding != null)
			{
				if (IsAttributeAccess(memberBinding))
				{
					AddProblem(memberBinding);
				}
			}

			base.VisitMemberBinding(memberBinding);
		}

		private bool IsAttributeAccess(MemberBinding memberBinding)
		{
			if (!(memberBinding.BoundMember is Method boundMember))
			{
				return false;
			}

			var isIndirectAccessItem = boundMember.DeclaringMember?.FullName?.Contains("Microsoft.Xrm.Sdk.Entity.Item") == true;

			if (isIndirectAccessItem)
			{
				return true;
			}

			var isCollectionAccess = boundMember.DeclaringMember?.FullName?
				.Contains("Microsoft.Xrm.Sdk.DataCollection`2<System.String,System.Object>.Item(System.String)") == true;

			if (!(memberBinding.TargetObject is MethodCall target) || !(target.Callee is MemberBinding calleeBinding)
				|| !(calleeBinding.BoundMember is Method getAttributesMethod))
			{
				return false;
			}

			var isAttributesAccess = isCollectionAccess
				&& getAttributesMethod.DeclaringMember?.FullName?.Contains("Microsoft.Xrm.Sdk.Entity.Attributes") == true;

			return isAttributesAccess;
		}
	}
}
