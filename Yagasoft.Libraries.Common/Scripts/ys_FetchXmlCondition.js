/// <reference path="Sdk.Soap.vsdoc.js" />
/// <reference path="notify.js" />
/// <reference path="jquery-1.11.1.js" />
/// <reference path="Xrm.Page.js" />
/// <reference path="SDK.Metadata.Query.vsdoc.js" />
/// <reference path="../CrmSchemaJs.js" />
/// <reference path="ys_CommonGeneric.js" />

function FetchXmlCondition_OnLoad()
{
	if (GetCrmVersion() === CrmVersion._2016)
	{
		SetupEntityNameAutoComplete();
	}

	EntityLogicalName_OnChange();
}

function EntityLogicalName_OnChange()
{
	setTimeout(SetEntityAdvancedFind, 100);
}

function SetEntityAdvancedFind()
{
	var entityLogicalName = GetFieldValue(Sdk.FetchXMLCondition.EntityLogicalName_ys_EntityLogicalName);

	if (entityLogicalName)
	{
		LoadAdvancedFind(Sdk.FetchXMLCondition.FetchXML, entityLogicalName);
	}
	else
	{
		RemoveAdvancedFind(Sdk.FetchXMLCondition.FetchXML);
	}
}

function SetupEntityNameAutoComplete()
{
	var result = [];
	var resultNames = [];

	ShowBusyIndicator('Loading entity names ... ', 736582);

	RetrieveEntityMetadata(null, ['LogicalName', 'DisplayName'], null, null, null,
		function (metadata)
		{
			HideBusyIndicator(736582);

			for (var i = 0; i < metadata.length; i++)
			{
				var e = metadata[i];
				var fieldName = e.LogicalName;

				result.push(
					{
						id: e,
						name: fieldName,
						name2: (e.DisplayName && e.DisplayName.UserLocalizedLabel && e.DisplayName.UserLocalizedLabel.Label)
								   ? e.DisplayName.UserLocalizedLabel.Label
								   : null
					});

				resultNames.push(fieldName);
			};

			SetupAutoComplete(Sdk.FetchXMLCondition.EntityLogicalName_ys_EntityLogicalName, result, resultNames, 10, true, true);
		},
		function (xhr, text)
		{
			HideBusyIndicator(736582);
			console.log(xhr);
			console.log(text);
		});
}
