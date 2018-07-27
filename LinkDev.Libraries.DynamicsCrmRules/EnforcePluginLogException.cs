//using System.Linq;
//using Microsoft.FxCop.Sdk;

//namespace LinkDev.Libraries.DynamicsCrmRules
//{
//	internal sealed class EnforcePluginLogException : BaseFxCopRule
//	{
//		public override TargetVisibilities TargetVisibility => TargetVisibilities.All;

//		public EnforcePluginLogException() : base("EnforcePluginLogException")
//		{

//		}

//		public override ProblemCollection Check(Member member)
//		{
//			var method = member as Method;

//			if (method == null || !method.FullName.Contains("ExecuteLogic"))
//			{
//				// This rule only applies to certain nodes.
//				// Return a null ProblemCollection so no violations are reported for this member.
//				return null;
//			}

//			Visit(method);

//			// By default the Problems collection is empty so no violations will be reported
//			// unless a check found and added a problem.
//			return Problems;
//		}

//		public override void VisitMemberBinding(MemberBinding memberBinding)
//		{
//			if (memberBinding != null)
//			{
//				if (ContainsThreading(memberBinding))
//				{
//					AddProblem(memberBinding);
//				}
//			}

//			base.VisitMemberBinding(memberBinding);
//		}

//		private bool ContainsThreading(MemberBinding binding)
//		{
//			return binding.Type?.Namespace?.Name?.Contains("System.Threading") == true;
//		}
//	}
//}
