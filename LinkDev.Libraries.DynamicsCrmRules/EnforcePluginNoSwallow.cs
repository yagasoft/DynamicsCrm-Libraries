using System.Diagnostics;
using System.Linq;
using Microsoft.FxCop.Sdk;

namespace LinkDev.Libraries.DynamicsCrmRules
{
	internal sealed class EnforcePluginNoSwallow : BaseFxCopRule
	{
		public override TargetVisibilities TargetVisibility => TargetVisibilities.All;

		public EnforcePluginNoSwallow() : base("EnforcePluginNoSwallow")
		{
			
		}

		public override ProblemCollection Check(Member member)
		{
			var method = member as Method;

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
		
		public override void VisitCatch(CatchNode catchNode)
		{

			if (catchNode != null)
			{
				if (!IsContainsThrow(catchNode.Block))
				{
					AddProblem(catchNode);
				}
			}

			base.VisitCatch(catchNode);
		}

		private bool IsContainsThrow(Block block)
		{
			if (block == null)
			{
				return false;
			}

			return block.Statements.Any(s => new[] { NodeType.Throw, NodeType.Rethrow }.Contains(s.NodeType))
				|| block.Statements.OfType<Block>().Any(IsContainsThrow)
				|| block.Statements.OfType<TryNode>().Any(n => n.Catchers.Select(c => c.Block).Any(IsContainsThrow));
		}
	}
}
