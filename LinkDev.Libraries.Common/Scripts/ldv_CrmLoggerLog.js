/// <reference path="ldv_CommonGeneric.js" />
/// <reference path="xrm.page.365.d.ts" />
var $ = parent.$ || window.$;

var LogPageNumber = '1';

function OnLoad()
{
	var type = Xrm.Page.getAttribute('ldv_regardingtype').getValue();
	var id = Xrm.Page.getAttribute('ldv_regardingid').getValue();

	if (type && id)
	{
		Xrm.Page.getAttribute('ldv_recordurl').setSubmitMode('never');
		Xrm.Page.getAttribute('ldv_recordurl').setValue(Xrm.Page.context.getClientUrl() + '/main.aspx?' +
			'etc=' + GetObjectTypeCode(type) + '&id=%7b' + id + '%7d' + '&newWindow=true&pagetype=entityrecord');
	}

	HighlightLogEntries();
}

function HighlightLogEntries()
{
	var documentObj = document;
	var startedRgb = [194, 255, 219];
	var finishedRgb = [209, 228, 255];
	var currentRgb = startedRgb;

	var highlightInner = function(label, colour, background)
	{
		var noBrElement = $('nobr', documentObj);

		var targetElement = noBrElement.filter(function()
		{
			return this.innerHTML === label;
		});

		if (targetElement.length <= 0)
		{
			targetElement = $('nobr > span', documentObj).filter(function()
			{
				return $(this).html() === label;
			}).parent();
		}

		targetElement.parent().parent().css({ 'color': colour, 'background-color': background });
	};

	var highlightFunctions = function()
	{
		var targetElement = $('div', documentObj).filter(function()
		{
			return $(this).parent().hasClass('ms-crm-List-DataCell-Lite')
				&& (this.innerHTML.indexOf('Started function: ') >= 0
					|| this.innerHTML.indexOf('Started: ') >= 0
					|| this.innerHTML.indexOf('Finished function: ') >= 0
					|| this.innerHTML.indexOf('Finished: ') >= 0);
		});

		var mod = 25;

		for (var i = 0; i < targetElement.length; i++)
		{
			var color = 'white';

			var isStart = targetElement[i].innerHTML.indexOf('Started function: ') >= 0
				|| targetElement[i].innerHTML.indexOf('Started: ') >= 0;
			var isFinish = targetElement[i].innerHTML.indexOf('Finished function: ') >= 0
				|| targetElement[i].innerHTML.indexOf('Finished: ') >= 0;

			if (i > 0 && targetElement[i - 1].innerHTML.split(':')[0]
				=== targetElement[i].innerHTML.split(':')[0])
			{
				// depth
				if (isStart)
				{
					currentRgb[0] -= mod;
					currentRgb[1] -= mod;
					currentRgb[2] -= mod;
				}

				// switch colours
				currentRgb = currentRgb === startedRgb ? finishedRgb : startedRgb;

				if (isFinish)
				{
					currentRgb[0] += mod;
					currentRgb[1] += mod;
					currentRgb[2] += mod;
				}
			}

			if (isStart)
			{
				color = currentRgb[1] < 140 ? 'white' : 'darkblue';
			}

			if (isFinish)
			{
				color = currentRgb[1] < 140 ? 'darkorange' : 'darkred';
			}

			var backgroundString = 'rgb(' + currentRgb + ')';
			$(targetElement[i]).parent().parent().css({ 'color': color, 'background-color': backgroundString });
		}
	};


	var highlightTimes = function()
	{
		var gridColumns = $('#gridBodyTable > thead > tr > th', documentObj);
		var durationColumnIndex = 0;

		// get the duration column index
		for (var j = 0; j < gridColumns.length; j++)
		{
			if (gridColumns[j].innerHTML.indexOf('Duration') >= 0)
			{
				durationColumnIndex = j;
				break;
			}
		}

		// get the duration and its associated row as a map
		var durationRowMap = $.map($('#gridBodyTable > tbody > tr', documentObj), function(e)
		{
			var durationCell = $(e, documentObj).find('td').eq(durationColumnIndex);
			var durationValueString = durationCell.find('div').html();

			var durationValue = 0;

			if (durationValueString != null)
			{
				durationValue = parseInt(durationValueString.replace(',', ''));
			}

			if (isNaN(durationValue))
			{
				durationValueString = durationCell.find('div > span').html();

				if (durationValueString != null)
				{
					durationValue = parseInt(durationValueString.replace(',', ''));
				}
			}

			return {
					duration: durationValue ? durationValue : 0,
					row: durationCell
				};
		});

		var durations = durationRowMap
			.map(function(e)
			{
				return e.duration;
			});

		var uniqueDurations = [];

		// get unique durations
		for (var m = 0; m < durations.length; m++)
		{
			if (uniqueDurations.indexOf(durations[m]) < 0)
			{
				uniqueDurations.push(durations[m]);
			}
		}

		uniqueDurations.sort(function(a, b)
		{
			return a - b;
		});

		var median = Median(uniqueDurations);

		// remove durations below median
		Remove(uniqueDurations, function(e)
		{
			return e < median;
		});

		var medianDistanceSum = Sum(uniqueDurations.map(function(e)
		{
			return e - median;
		}));

		var modRange = 255 - 140;

		// get the modifier as a function of distance from median
		for (var l = 0; l < durationRowMap.length; l++)
		{
			var rowMap = durationRowMap[l];

			if (uniqueDurations.indexOf(rowMap.duration) < 0)
			{
				rowMap.mod = 0;
				continue;
			}

			var ratio = (rowMap.duration - median) / medianDistanceSum;
			rowMap.mod = Math.ceil(-ratio * modRange);
		}

		for (var k = 0; k < durationRowMap.length; k++)
		{
			var currentMod = durationRowMap[k].mod;
			var newRgb = [finishedRgb[0] + currentMod, finishedRgb[1] + currentMod, finishedRgb[2] + currentMod];
			var backgroundRgbString = 'rgb(' + newRgb + ')';

			durationRowMap[k].row.css({ 'color': 'black', 'background-color': backgroundRgbString });
		}
	};

	var highlight = function()
	{
		highlightInner('Error', 'white', 'red');
		highlightInner('Warning', 'black', 'yellow');
		highlightInner('Debug', 'deeppink', 'white');

		highlightFunctions();
		highlightTimes();
	};

	var clickHandler = function()
	{
		var pagingLoop = function()
		{
			var currentNum = $('#_PageNum', documentObj).html();

			if (LogPageNumber === currentNum)
			{
				setTimeout(pagingLoop, 200);
			}
			else
			{
				LogPageNumber = currentNum;
				highlight();
			}
		};
		pagingLoop();
	};

	var highlightLoop = function()
	{
		if (GetControl('Log_Entries'))
		{
			setTimeout(function()
			{
				var noBrElement = $('nobr', documentObj);

				if (noBrElement.length <= 0)
				{
					documentObj = parent.document;
				}

				highlight();

				$('#page_FL0', documentObj).click(clickHandler);
				$('#page_L0', documentObj).click(clickHandler);
				$('#page_R0', documentObj).click(clickHandler);
			}, 1000);
		}
		else
		{
			setTimeout(highlightLoop, 500);
		}
	};

	highlightLoop();
}

function LogTree_OnStateChange(context)
{
	if (context.getEventSource().getDisplayState() === 'collapsed')
	{
		setTimeout(
			function()
			{
				var section = $('#\\{21ce4317-c55d-e300-164c-a0ee06c9aa98\\}', parent.document);
				section.find('.ms-crm-Field-Data-Print:eq(1)').remove();
				section.find('.ms-crm-Field-Data-Print:eq(0)').append('<iframe id="iFrame_clt"' +
					'src="' + Xrm.Page.context.getClientUrl() +
					'/WebResources/ldv_/CrmLogger/html/Tree.html" frameborder="0" scrolling="yes" style="height:350px"></iframe>');
			}, 500);
	}
}