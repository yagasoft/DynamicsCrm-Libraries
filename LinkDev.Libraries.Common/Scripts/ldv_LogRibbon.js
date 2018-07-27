/// <reference path="ldv_CommonGeneric.js" />
/// <reference path="Sdk.Soap.vsdoc.js" />
/// <reference path="jquery-1.11.1.js" />
/// <reference path="Sdk.Soap.vsdoc.js" />
/// <reference path="Xrm.Page.js" />

var LogPageNumber = '1';

function HighlightRows()
{
	LoadWebResources('ldv_CommonGenericJs',
		function()
		{
			var documentObj = document;
			var rgb = [209, 228, 255];

			//var highlightInner = function(label, colour, background)
			//{
			//	var noBrElement = $('nobr', documentObj);

			//	var targetElement = noBrElement.filter(function()
			//	{
			//		return this.innerHTML === label;
			//	});

			//	if (targetElement.length <= 0)
			//	{
			//		targetElement = $('nobr > span', documentObj).filter(function()
			//		{
			//			return $(this).html() === label;
			//		}).parent();
			//	}

			//	targetElement.parent().parent().css({ 'color': colour, 'background-color': background });
			//};

			var highlightTimes = function ()
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
				var durationRowMap = $.map($('#gridBodyTable > tbody > tr', documentObj), function (e)
				{
					var durationCell = $(e, documentObj).find('td').eq(durationColumnIndex);
					var durationValueString = durationCell.find('nobr').html();

					var durationValue = 0;

					if (durationValueString != null)
					{
						durationValue = parseInt(durationValueString.replace(',', ''));
					}

					if (isNaN(durationValue))
					{
						durationValueString = durationCell.find('nobr > span').html();

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
					var newRgb = [rgb[0] + currentMod, rgb[1] + currentMod, rgb[2] + currentMod];
					var backgroundRgbString = 'rgb(' + newRgb + ')';

					durationRowMap[k].row.css({ 'color': 'black', 'background-color': backgroundRgbString });
				}
			};

			var highlightFails = function ()
			{
				var noBrElement = $('nobr', documentObj);

				var targetElement = noBrElement.filter(function()
				{
					return this.innerHTML === 'Failure';
				});

				if (targetElement.length <= 0)
				{
					targetElement = $('nobr > span', documentObj).filter(function()
					{
						return $(this).html() === 'Failure';
					}).parent();
				}

				targetElement.parent().css({ 'color': 'white', 'background-color': '#a72525' });
			};

			var highlight = function()
			{
				highlightFails();
				highlightTimes();
			};

			var clickHandler = function ()
			{
				var pagingLoop = function ()
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

			var noBrElement = $('nobr', documentObj);

			if (noBrElement.length <= 0)
			{
				documentObj = parent.document;
			}

			highlight();

			$('#page_FL0', documentObj).click(clickHandler);
			$('#page_L0', documentObj).click(clickHandler);
			$('#page_R0', documentObj).click(clickHandler);

			highlight();
		});

	return false;
}

// credit: http://stackoverflow.com/a/950146/1919456
function LoadScript(url, callback)
{
	// Adding the script tag to the head as suggested before
	var head = document.getElementsByTagName('head')[0];
	var script = document.createElement('script');
	script.type = 'text/javascript';
	script.src = url;

	// Then bind the event to the callback function.
	// There are several events for cross browser compatibility.
	//script.onreadystatechange = callback;
	script.onload = callback;

	// Fire the loading
	head.appendChild(script);
}

function LoadWebResources(resources, callback)
{
	/// <summary>
	///     Author: Ahmed el-Sawalhy
	/// </summary>
	/// <param name="resources" type="String | String[]" optional="false"></param>
	/// <param name="callback" type="Function" optional="true"></param>
	if (resources.length <= 0)
	{
		callback();
		return;
	}

	if (typeof resources === 'string')
	{
		resources = [resources];
	}

	var localCallback = function ()
	{
		if (resources.length > 1)
		{
			LoadWebResources(resources.slice(1, resources.length), callback);
		}
		else
		{
			callback();
		}
	};
	LoadScript(Xrm.Page.context.getClientUrl() + '/WebResources/' + resources[0], localCallback);
}
