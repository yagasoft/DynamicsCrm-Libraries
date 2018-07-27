using System.Diagnostics;
using System.Linq;
using Microsoft.FxCop.Sdk;

namespace LinkDev.Libraries.DynamicsCrmRules
{
	internal sealed class EnforcePluginNoExecuteBulk : BaseFxCopRule
	{
		public override TargetVisibilities TargetVisibility => TargetVisibilities.All;

		public EnforcePluginNoExecuteBulk() : base("EnforcePluginNoExecuteBulk")
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
		
		public override void VisitConstruct(Construct construct)
		{
			if (construct != null)
			{
				var metadataCreateMessages =
					new[]
					{
						"Microsoft.Xrm.Sdk.Messages.ExecuteMultipleRequest",
						"Microsoft.Xrm.Sdk.Messages.ExecuteTransactionRequest"
					};

				if (metadataCreateMessages.Contains(construct.Type.FullName))
				{
					AddProblem(construct);
				}
			}

			base.VisitConstruct(construct);
		}
	}
}
