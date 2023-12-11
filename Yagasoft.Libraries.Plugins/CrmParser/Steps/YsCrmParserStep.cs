﻿// this file was generated by the xRM Test Framework VS Extension

#region Imports

using System;
using System.Activities;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata.Query;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;

using Yagasoft.Libraries.Common;
using Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.Base;
using Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.Planning;
using Yagasoft.Libraries.EnhancedOrgService.ExecutionPlan.SerialiseWorkarounds;

using static Microsoft.IdentityModel.Protocols.WSTrust.WSTrustServiceContractConstants;


#endregion

namespace Yagasoft.Libraries.Steps
{
	/// <summary>
	///     Parse the constructs in the given text.<br />
	///     Version: 2.1.1
	/// </summary>
	public class YsCrmParserStep : CodeActivity
	{
		#region Arguments

		[Input("Text")]
		public InArgument<string> Text { get; set; }

		[Input("Compressed Text")]
		public InArgument<string> CompressedText { get; set; }

		[Output("Parsed Text")]
		public OutArgument<string> ParsedText { get; set; }

		[Output("Parsed Compressed Text")]
		public OutArgument<string> ParsedCompressedText { get; set; }

		#endregion

		protected override void Execute(CodeActivityContext context)
		{
			new YsCrmParserStepLogic().Execute(this, context, PluginUser.ContextUser);
		}
	}

	internal class YsCrmParserStepLogic : StepLogic<YsCrmParserStep>
	{
		protected override void ExecuteLogic()
		{
			var text = ExecutionContext.GetValue(codeActivity.Text);
			var compressedText = ExecutionContext.GetValue(codeActivity.CompressedText);

			if (compressedText.IsFilled())
			{
				text = compressedText.Decompress();
			}

			text.Require(nameof(codeActivity.Text));

			var image = Context.PrimaryEntityName.IsFilled() ? GetPostImage<Entity>() : null;
			var result = image == null
				? CrmParser.Interpreter.Parse(text).Evaluate(Service)
				: CrmParser.Interpreter.Parse(text).Evaluate(Service, image);

			if (compressedText.IsFilled())
			{
				ExecutionContext.SetValue(codeActivity.ParsedCompressedText, result.Compress());
			}
			else
			{
				ExecutionContext.SetValue(codeActivity.ParsedText, result);
			}
		}
	}
}
