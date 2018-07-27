/// <reference path="Sdk.Soap.vsdoc.js" />
/// <reference path="ldv_CommonGeneric.js" />
/// <reference path="../CrmSchemaJs.js" />

var Ldv = window.Ldv || {};

Ldv.GenericConfigForm_OnLoad = function()
{
    Ldv.SetupStageAutoComplete(function () { HideBusyIndicator(651841); });
};

Ldv.SetupStageAutoComplete = function(callback)
{
    var resultMap = [];

    ShowBusyIndicator('Loading calendars ... ', 651841);

    $.ajax({
        type: "GET",
        contentType: "application/json; charset=utf-8",
        datatype: "json",
        url: Xrm.Page.context.getClientUrl() + "/api/data/v8.2/calendars?$select=calendarid,name&$filter=type eq 1",
        beforeSend: function(xmlHttpRequest)
        {
            xmlHttpRequest.setRequestHeader("OData-MaxVersion", "4.0");
            xmlHttpRequest.setRequestHeader("OData-Version", "4.0");
            xmlHttpRequest.setRequestHeader("Accept", "application/json");
        },
        async: true,
        success: function(data, textStatus, xhr)
        {
            var results = data.value;

            for (var i = 0; i < results.length; i++)
            {
                var calendarid = results[i]["calendarid"];
                var name = results[i]["name"];

                resultMap.push(
                    {
                        text: name,
                        value: calendarid
                    });
            }

            resultMap.sort(
                function(e1, e2)
                    {
                        var keyA = e1.text;
                        var keyB = e2.text;
                        return keyA < keyB ? -1 : (keyA > keyB ? 1 : 0);
                    });

            LoadMultiSelect(Sdk.GenericConfiguration.DefaultCalendar, resultMap, 'Default Calendar', 170, callback,
                true, true, false, true);
        },
        error: function(xhr, textStatus, errorThrown)
        {
            HideBusyIndicator(651841);
            console.error(xhr);
            console.error(textStatus + ": " + errorThrown);
        }
    });
};
