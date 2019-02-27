/// <reference path="ldv_CommonGeneric.js" />
/// <reference path="Sdk.Soap.vsdoc.js" />
/// <reference path="jquery-1.11.1.js" />
/// <reference path="Sdk.Soap.vsdoc.js" />
/// <reference path="Xrm.Page.js" />
/// <reference path="../CrmSchemaJs.js" />

var FormTabNames = {
	EVERY_MINUTE_PATTERN: 'EveryMinutePatternTab',
	HOURLY_PATTERN: 'HourlyPatternTab',
	DAILY_PATTERN: 'DailyPatternTab',
	WEEKLY_PATTERN: 'WeeklyPatternTab',
	MONTHLY_PATTERN: 'MonthlyPatternTab'
};

var FormSectionNames = {
	MONTHLY_DAY_PATTERN: 'MonthlyDayPatternSection',
	MOTHLY_WEEK_DAY_PATTERN: 'MonthlyWeekDayPatternSection'
};

function Recurrence_OnLoad()
{
	Date_OnChange();
	RecurrencePattern_OnChange(true);
	MonthlyPattern_OnChange(true);
}

function Date_OnChange()
{
	ValidateDateFieldsOrder(Sdk.RecurrenceRule.StartDate, Sdk.RecurrenceRule.EndDate);
}

function RecurrencePattern_OnChange(isOnLoad)
{
	var option = GetFieldValue(Sdk.RecurrenceRule.RecurrencePattern);

	HideAllPatternTabs();

	if (option)
	{
		switch (option)
		{
			case Sdk.RecurrenceRule.RecurrencePatternEnum.EveryMinute:
				SetTabVisible(FormTabNames.EVERY_MINUTE_PATTERN, true);
				if (!isOnLoad) ResetAllPatternTabsExcept(FormTabNames.EVERY_MINUTE_PATTERN);
				SetFieldRequired(Sdk.RecurrenceRule.MinuteFrequency, true);
				break;

			case Sdk.RecurrenceRule.RecurrencePatternEnum.Hourly:
				SetTabVisible(FormTabNames.HOURLY_PATTERN, true);
				if (!isOnLoad) ResetAllPatternTabsExcept(FormTabNames.HOURLY_PATTERN);
				SetFieldRequired(Sdk.RecurrenceRule.HourlyFrequency, true);
				break;

            case Sdk.RecurrenceRule.RecurrencePatternEnum.Daily:
				SetTabVisible(FormTabNames.DAILY_PATTERN, true);
				if (!isOnLoad) ResetAllPatternTabsExcept(FormTabNames.DAILY_PATTERN);
				SetFieldRequired(Sdk.RecurrenceRule.DailyFrequency, true);
				break;

            case Sdk.RecurrenceRule.RecurrencePatternEnum.Weekly:
				SetTabVisible(FormTabNames.WEEKLY_PATTERN, true);
				LoadOptionSetMultiSelection(Sdk.RecurrenceRule.WeekDays, Sdk.RecurrenceRule.WeekDay, 'Days');
				if (!isOnLoad) ResetAllPatternTabsExcept(FormTabNames.WEEKLY_PATTERN);
				SetFieldRequired(Sdk.RecurrenceRule.WeeklyFrequency, true);
				SetFieldRequired(Sdk.RecurrenceRule.WeekDays, true);
				break;

            case Sdk.RecurrenceRule.RecurrencePatternEnum.Monthly:
				SetTabVisible(FormTabNames.MONTHLY_PATTERN, true);
				if (!isOnLoad) ResetAllPatternTabsExcept(FormTabNames.MONTHLY_PATTERN);
				SetFieldRequired(Sdk.RecurrenceRule.MonthlyPattern, true);
				SetFieldRequired(Sdk.RecurrenceRule.Months, true);
				break;
		}
	}

	MonthlyPattern_OnChange(isOnLoad);
}

function MonthlyPattern_OnChange(isOnLoad)
{
	var monthlyPattern = GetFieldValue(Sdk.RecurrenceRule.MonthlyPattern);

	SetSectionVisible('MonthlyPatternTab', 'MonthlyDayPatternSection', false);
	SetSectionVisible('MonthlyPatternTab', 'MonthlyWeekDayPatternSection', false);

	LoadOptionSetMultiSelection(Sdk.RecurrenceRule.Months, Sdk.RecurrenceRule.MonthOfYear, 'Months');

    if (monthlyPattern === Sdk.RecurrenceRule.MonthlyPatternEnum.SpecificDays)
	{
		if (!isOnLoad)
		{
			SetFieldValue(Sdk.RecurrenceRule.DayOccurrences, null, true);
			SetFieldValue(Sdk.RecurrenceRule.WeekDays, null, true);
		}

		SetSectionVisible('MonthlyPatternTab', 'MonthlyDayPatternSection', true);
		SetSectionVisible('MonthlyPatternTab', 'MonthlyWeekDayPatternSection', false);

		var days = [];

		for (var i = 1 ; i <= 31 ; i++)
		{
			days.push({ text: i + '', value: i + '' });
		}

		LoadMultiSelect(Sdk.RecurrenceRuleException.DaysOfTheMonth, days, 'Days', 150, null, false, true);

		SetFieldRequired(Sdk.RecurrenceRule.DayOccurrences, false);
		SetFieldRequired(Sdk.RecurrenceRule.WeekDays, false);
		SetFieldRequired(Sdk.RecurrenceRule.DaysOfTheMonth, true);
	}
    else if (monthlyPattern === Sdk.RecurrenceRule.MonthlyPatternEnum.DayOccurrence)
	{
		if (!isOnLoad)
		{
			SetFieldValue(Sdk.RecurrenceRule.DaysOfTheMonth, null, true);
		}

		SetSectionVisible('MonthlyPatternTab', 'MonthlyDayPatternSection', false);
		SetSectionVisible('MonthlyPatternTab', 'MonthlyWeekDayPatternSection', true);

		LoadOptionSetMultiSelection(Sdk.RecurrenceRule.DayOccurrences,
			Sdk.RecurrenceRule.MonthlyDayOccurrence, 'On the');
		LoadOptionSetMultiSelection(Sdk.RecurrenceRule.WeekDays + ':1',
			Sdk.RecurrenceRule.WeekDay, 'occurrence of');

		SetFieldRequired(Sdk.RecurrenceRule.DaysOfTheMonth, false);
		SetFieldRequired(Sdk.RecurrenceRule.DayOccurrences, true);
		SetFieldRequired(Sdk.RecurrenceRule.WeekDays, true);
	}
}

function HideAllPatternTabs()
{
	SetTabVisible(FormTabNames.EVERY_MINUTE_PATTERN, false);
	SetTabVisible(FormTabNames.HOURLY_PATTERN, false);
	SetTabVisible(FormTabNames.DAILY_PATTERN, false);
	SetTabVisible(FormTabNames.WEEKLY_PATTERN, false);
	SetTabVisible(FormTabNames.MONTHLY_PATTERN, false);

	RemoveMultiSelect(Sdk.RecurrenceRule.WeekDays);
	RemoveMultiSelect(Sdk.RecurrenceRule.WeekDays + '1');
	RemoveMultiSelect(Sdk.RecurrenceRule.Months);
	RemoveMultiSelect(Sdk.RecurrenceRule.DaysOfTheMonth);
}

function ResetAllPatternTabsExcept(tabException)
{
	for (var tabNameProperty in FormTabNames)
	{
		if (FormTabNames.hasOwnProperty(tabNameProperty))
		{
			var tabName = FormTabNames[tabNameProperty];

			if (tabName === tabException)
			{
				continue;
			}

			switch (tabName)
			{
				case FormTabNames.EVERY_MINUTE_PATTERN:
					SetFieldValue(Sdk.RecurrenceRule.MinuteFrequency, null, true);
					SetFieldRequired(Sdk.RecurrenceRule.MinuteFrequency, false);
					break;

				case FormTabNames.HOURLY_PATTERN:
					SetFieldValue(Sdk.RecurrenceRule.HourlyFrequency, null, true);
					SetFieldRequired(Sdk.RecurrenceRule.HourlyFrequency, false);
					break;

				case FormTabNames.DAILY_PATTERN:
					SetFieldValue(Sdk.RecurrenceRule.DailyFrequency, null, true);
					SetFieldRequired(Sdk.RecurrenceRule.DailyFrequency, false);
					break;

				case FormTabNames.WEEKLY_PATTERN:
					SetFieldValue(Sdk.RecurrenceRule.WeeklyFrequency, null, true);
					SetFieldRequired(Sdk.RecurrenceRule.WeeklyFrequency, false);
					
					if (tabException !== FormTabNames.MONTHLY_PATTERN)
					{
						SetFieldValue(Sdk.RecurrenceRule.WeekDays, null, true);
						SetFieldRequired(Sdk.RecurrenceRule.WeekDays, false);
					}

					break;

				case FormTabNames.MONTHLY_PATTERN:
					SetFieldValue(Sdk.RecurrenceRule.MonthlyPattern, null, true);
					SetFieldValue(Sdk.RecurrenceRule.Months, null, true);
					SetFieldValue(Sdk.RecurrenceRule.DaysOfTheMonth, null, true);
					SetFieldValue(Sdk.RecurrenceRule.DayOccurrences, null, true);

					SetFieldRequired(Sdk.RecurrenceRule.MonthlyPattern, false);
					SetFieldRequired(Sdk.RecurrenceRule.Months, false);
					SetFieldRequired(Sdk.RecurrenceRule.DaysOfTheMonth, false);
					SetFieldRequired(Sdk.RecurrenceRule.DayOccurrences, false);

					if (tabException !== FormTabNames.WEEKLY_PATTERN)
					{
						SetFieldValue(Sdk.RecurrenceRule.WeekDays, null, true);
						SetFieldRequired(Sdk.RecurrenceRule.WeekDays, false);
					}

					break;
			}
		}
	}
}

function LoadOptionSetMultiSelection(fieldName, optionSetName, message)
{
	var xrmOptions = Xrm.Page.getAttribute(optionSetName).getOptions();

	var options = $.map(xrmOptions,
		function(element)
		{
			return { text: element.text, value: element.text.toLowerCase() };
		});

	Remove(options, 'value', '');
	LoadMultiSelect(fieldName, options, message, 150, null, false, true);
}
