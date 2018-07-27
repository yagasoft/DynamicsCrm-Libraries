using System.Diagnostics;
using System.Linq;
using Microsoft.FxCop.Sdk;

namespace LinkDev.Libraries.DynamicsCrmRules
{

	internal abstract class BaseFxCopRule : BaseIntrospectionRule
	{
		protected BaseFxCopRule(string ruleName)
			: base(ruleName, "LinkDev.Libraries.DynamicsCrmRules.RuleMetadata", typeof(BaseFxCopRule).Assembly)
		{
		}

		protected void AddProblem(Node violatingNode, params object[] resolutionParameters)
		{
			var problem = new Problem(GetResolution(resolutionParameters), violatingNode);

			if (Problems.Any(p => p.Id == problem.Id && p.SourceFile == problem.SourceFile && p.SourceLine == problem.SourceLine))
			{
				return;
			}

			Problems.Add(problem);
		}

		protected bool IsPlugin(ClassNode classNode)
		{
			return (classNode.Interfaces?.Any(i => i.Name.Name.Contains("IPlugin")) ?? false)
				|| (classNode.BaseClass != null && IsPlugin(classNode.BaseClass));
		}

		protected bool IsActivity(ClassNode classNode)
		{
			return (classNode.BaseClass?.FullName.Contains("CodeActivity") ?? false)
				|| (classNode.BaseClass != null && IsActivity(classNode.BaseClass));
		}

		protected bool IsPluginOrActivity(TypeNode typeNode)
		{
			return typeNode.NodeType == NodeType.Class && (IsPlugin((ClassNode) typeNode) || IsActivity((ClassNode) typeNode));
		}

		protected bool IsContainsPluginOrActivity(AssemblyNode assmeblyNode)
		{
			return assmeblyNode.Types.Any(IsPluginOrActivity);
		}

		protected bool IsUserCode(TypeNode typeNode)
		{
			return !(typeNode.HasCustomAttribute(typeof(DebuggerNonUserCodeAttribute).FullName)
				|| (typeNode.DeclaringType != null && !IsUserCode(typeNode.DeclaringType)));
		}
	}
}