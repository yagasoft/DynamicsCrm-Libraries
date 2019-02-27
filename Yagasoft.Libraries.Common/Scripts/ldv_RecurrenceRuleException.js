/// <reference path="ldv_CommonGeneric.js" />
/// <reference path="Sdk.Soap.vsdoc.js" />
/// <reference path="jquery-1.11.1.js" />
/// <reference path="Sdk.Soap.vsdoc.js" />
/// <reference path="Xrm.Page.js" />
/// <reference path="../CrmSchemaJs.js" />
function Recurrence_OnLoad()
{
	Date_OnChange();

	var minutes = [];

	for (var i = 0; i <= 59; i++)
	{
		minutes.push({ text: i + '', value: i + '' });
	}

	LoadMultiSelect(Sdk.RecurrenceRuleException.Minutes, minutes, 'Minutes', 300, null, false, true);

	var hours = [];

	for (var i = 0; i <= 23; i++)
	{
		var text;

		if (i === 0)
		{
			text = '12 AM';
		}
		else if (i > 0 && i < 12)
		{
			text = i + ' AM';
		}
		else
		{
			text = i + ' PM';
		}

		hours.push({ text: text, value: i + '' });
	}

	LoadMultiSelect(Sdk.RecurrenceRuleException.Hours, hours, 'Hours', 240, null, false, true);

	var days = [];

	for (var i = 1; i <= 31; i++)
	{
		days.push({ text: i + '', value: i + '' });
	}

	LoadMultiSelect(Sdk.RecurrenceRuleException.DaysOfTheMonth, days, 'Days', 320, null, false, true);
	LoadSelection(Sdk.RecurrenceRuleException.WeekDays, Sdk.RecurrenceRuleException.WeekDay, 'Week Days');
	LoadSelection(Sdk.RecurrenceRuleException.Months, Sdk.RecurrenceRuleException.Month, 'Months');

	var years = [];

	for (var i = 2000; i <= 2050; i++)
	{
		years.push({ text: i + '', value: i + '' });
	}

	LoadMultiSelect(Sdk.RecurrenceRuleException.Years, years, 'Years', 300, null, false, true);
	LoadOptionSetMultiSelection(Sdk.RecurrenceRuleException.DayOccurrences,
		Sdk.RecurrenceRuleException.MonthlyDayOccurrence, 'On the');
}

function Date_OnChange()
{
	ValidateDateFieldsOrder(Sdk.RecurrenceRuleException.StartDate, Sdk.RecurrenceRuleException.EndDate);
}

function LoadSelection(fieldName, optionSetName, title)
{
	var xrmOptions = Xrm.Page.getAttribute(optionSetName).getOptions();

	var options = $.map(xrmOptions, function(element)
	{
		return { text: element.text, value: element.text.toLowerCase() };
	});

	Remove(options, 'value', '');
	LoadMultiSelect(fieldName, options, title, 999, null, false, true);
}

function LoadOptionSetMultiSelection(fieldName, optionSetName, message)
{
	var xrmOptions = Xrm.Page.getAttribute(optionSetName).getOptions();

	var options = $.map(xrmOptions, function (element)
	{
		return { text: element.text, value: element.text.toLowerCase() };
	});

	Remove(options, 'value', '');
	LoadMultiSelect(fieldName, options, message, 150, null, false, true);
}
