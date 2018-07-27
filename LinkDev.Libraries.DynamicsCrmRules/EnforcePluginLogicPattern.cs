using System.Diagnostics;
using System.Linq;
using Microsoft.FxCop.Sdk;

namespace LinkDev.Libraries.DynamicsCrmRules
{
	internal sealed class EnforcePluginLogicPattern : BaseFxCopRule
	{
		public override TargetVisibilities TargetVisibility => TargetVisibilities.All;

		public EnforcePluginLogicPattern() : base("EnforcePluginLogicPattern")
		{

		}

		public override ProblemCollection Check(TypeNode member)
		{
			var classNode = member as ClassNode;

			if (classNode == null || !IsUserCode(member))
			{
				// This rule only applies to certain nodes.
				// Return a null ProblemCollection so no violations are reported for this member.
				return null;
			}

			var currentClass = classNode;

			while (currentClass != null)
			{
				if (IsPlugin(currentClass))
				{
					CheckPluginClassMembers(currentClass);
					break;
				}

				if (IsActivity(currentClass))
				{
					CheckActivityClassMembers(currentClass);
					break;
				}

				currentClass = currentClass.BaseClass;
			}

			// By default the Problems collection is empty so no violations will be reported
			// unless a check found and added a problem.
			return Problems;
		}

		private void CheckPluginClassMembers(ClassNode classNode)
		{
			if (classNode == null)
			{
				return;
			}

			if (classNode.Members.Any(m => m.NodeType == NodeType.Property || m.NodeType == NodeType.Field))
			{
				AddProblem(classNode, classNode.FullName);
			}
		}

		private void CheckActivityClassMembers(ClassNode classNode)
		{
			var validMembers = classNode.Members
				.Where(m =>
					   {
						   switch (m.NodeType)
						   {
							   case NodeType.Field:
								   return !((Field) m).Type.FullName.Contains("System.Activities");
							   case NodeType.Property:
								   return !((PropertyNode) m).Type.FullName.Contains("System.Activities");
						   }

						   return false;
					   });

			if (validMembers.Any(m => m.NodeType == NodeType.Property || m.NodeType == NodeType.Field))
			{
				AddProblem(classNode, classNode.FullName);
			}
		}
	}
}
