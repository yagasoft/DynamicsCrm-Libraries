/// <reference path="Sdk.Soap.vsdoc.js" />
/// <reference path="jquery-1.11.1.js" />
/// <reference path="Sdk.Soap.vsdoc.js" />
/// <reference path="xrm.page.365.d.ts" />

// Author: Ahmed Elsawalhy
// Version: 1.11.6

var IsCommonGenericLibraryLoaded = true;
var $ = window.$ || parent.$;

window.onerror = function(msg, url, line)
{
	console.log("Message : " + msg);
	console.log("url : " + url);
	console.log("Line number : " + line);
};

/////////////////////////////////////////////////////
//#region >>>>>>>>> UI custom controls <<<<<<<<<<< //

function LoadRichEditor(fieldName, suffix, callback)
{
	/// <summary>
	///     Appends the rich editor frame to the specified field and hides the field using the DOM.<br />
	///     Author: Ahmed Elsawalhy, Tarek Selem
	/// </summary>
	/// <param name="fieldName" type="String">The logical name of the field to replace.</param>
	/// <param name="suffix" type="String">Editor frame DIV suffix. Must be passed if more than one editor on form.</param>
	if (!fieldName)
	{
		var message = 'Parameter values are not set correctly.';
		console.error(message);
		throw message;
	}

	suffix = suffix || '';

	var fieldContainer = GetInsertionRow(fieldName);

	if (!fieldContainer)
	{
		return;
	}

	var isSkipFireOnChange = false;

	var editorJsUrl = Xrm.Page.context.getClientUrl() + '/WebResources/ldv_/CkRichEditor/ckeditor.js';

    // add the control that is going to be replaced by CK Editor
	fieldContainer.before('<tr><td colspan="2">' +
		'<textarea name="ckWysiwyg_' + suffix + '" id="ckWysiwyg_' + suffix + '"></textarea>' +
		'</td></tr>');

	// load CKEditor script
	var headTag = parent.document.getElementsByTagName("head")[0];
    var jqTag = parent.document.createElement('script');
	jqTag.type = 'text/javascript';
	jqTag.src = editorJsUrl;

	// form the editor and its events
	jqTag.onload = function ()
	{
		parent.CKEDITOR.replace('ckWysiwyg_' + suffix);

		var editor = parent.CKEDITOR.dom.element.get('ckWysiwyg_' + suffix).getEditor();

		if (editor)
		{
			// initial editor data copied from field
			var setEditorData = function ()
			{
				// prevent looping
				if (isSkipFireOnChange)
				{
					isSkipFireOnChange = false;
					return;
				}

				parent.CKEDITOR.instances['ckWysiwyg_' + suffix].setData(Xrm.Page.getAttribute(fieldName).getValue());

                var editorFindLoop =
					function()
					{
						var editorElement = $('.cke_wysiwyg_frame', parent.document);

						if (editorElement.length)
						{
							$(editorElement[0].contentWindow).bind('keydown', function(event)
							{
								if (event.ctrlKey || event.metaKey)
								{
									switch (String.fromCharCode(event.which).toLowerCase())
									{
										case 's':
											setTimeout(
												function()
												{
													Xrm.Page.data.save();
												}, 100);

											event.preventDefault();
											break;
									}
								}
							});
						}
						else
						{
							setTimeout(editorFindLoop, 500);
						}
					}

				editorFindLoop();
			};

			// add an OnChange event to copy field data to editor
            Xrm.Page.getAttribute(fieldName).addOnChange(setEditorData);

			setEditorData();

			// add an OnChange event to copy editor data to field
			editor.on('change',
				function ()
				{
                    Xrm.Page.getAttribute(fieldName).setValue(editor.getData());
					// prevent looping
					isSkipFireOnChange = true;
                    Xrm.Page.getAttribute(fieldName).fireOnChange();
				});
		}

		if (callback)
		{
			callback();
		}
	};

    headTag.appendChild(jqTag);

	var hide = function(fieldName)
	{
		var fieldContainer = GetFieldContainer(fieldName);

		if (fieldContainer == null)
		{
			return;
		}

		var found = false;
		var set = false;
		var trs = $('tr', fieldContainer.parent());

		trs.each(function(tr)
		{
			if (set)
			{
				return;
			}

			if (trs[tr].innerHTML === fieldContainer[0].innerHTML)
			{
				$(trs[tr]).hide();
				found = true;
				return;
			}

			if (found && !trs[tr].innerHTML)
			{
				$(trs[tr]).hide();
			}

			if (found && trs[tr].innerHTML)
			{
				set = true;
			}
		});
	};

	hide(fieldName);
}

var AdvancedFindMap = window.AdvancedFindMap || {};

function LoadAdvancedFind(fieldName, logicalName, height, entityNameFieldName)
{
	/// <summary>
	///     Appends the advanced find frame to the specified field using the DOM.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	/// <param name="fieldName" type="String">The logical name of the field to replace.</param>
	/// <param name="logicalName" type="String">
	///     The logical name of the entity to apply conditions.
	///     This can be null if 'entityNameFieldName' is passed instead.
	/// </param>
	/// <param name="height" type="Number">[OPTIONAL=200]The height of the advanced find frame.</param>
	/// <param name="entityNameFieldName" type="String">[OPTIONAL]The logical name of the entity name field.</param>
	if (!fieldName || (!logicalName && !entityNameFieldName))
	{
		var message = 'Parameter values are not set correctly.';
		console.error(message);
		throw message;
	}

	// in auto-complete fields with an event change, the incomplete value is sent, then the correct one in another event
	// use this option to read the entity name from the field directly with a delay to get it right
	if (entityNameFieldName)
	{
		setTimeout(function()
		{
			LoadAdvancedFind(fieldName, GetFieldValue(entityNameFieldName), height);
		}, 200);

		return;
	}

	RemoveAdvancedFind(fieldName);

	var fieldContainer = GetInsertionRow(fieldName);

	if (!fieldContainer)
	{
		return;
	}

	if (typeof parent !== "undefined")
	{
		parent.AdvancedFindMap = AdvancedFindMap;
	}

	var iFrameUrl = Xrm.Page.context.getClientUrl() + '/WebResources/ldv_AdvancedFindHtml#' + fieldName +
        '#' + logicalName + '#' + 5;

	// insert editor frame right after the field as a new row
	fieldContainer.before('<tr id="advancedFindRow_' + fieldName + '"><td colspan="2">' +
		'<iframe id="advancedFindFrame_' + fieldName + '"' +
		'src="' +iFrameUrl + '" frameborder="0" scrolling="no"></iframe>' +
		'</td></tr>');
}

function IsAdvancedFindLoaded(fieldName)
{
	/// <summary>
	///     Checks whether the advanced-find frame is loaded on the given field or not.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	var oldFrame = $('#advancedFindFrame_' + fieldName, parent.document);

	// CRM 2015-
	if (oldFrame.length <= 0)
	{
		oldFrame = $('#advancedFindFrame_' + fieldName);
	}

	return oldFrame.length > 0;
}

function RemoveAdvancedFind(fieldName)
{
	/// <summary>
	///     Removes the frame from the form by deleting its DOM elements.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	var oldFrameRow = $('#advancedFindRow_' + fieldName, parent.document);

	// CRM 2015-
	if (oldFrameRow.length <= 0)
	{
		oldFrameRow = $('#advancedFindRow_' + fieldName);
	}

	if (oldFrameRow.length > 0)
	{
		oldFrameRow.remove();
	}

	if (AdvancedFindMap[fieldName])
	{
		RemoveOnChange(fieldName, AdvancedFindMap[fieldName]);
	}
}

function LoadAutoAdvancedFind(fieldName, entityNameFieldName, height, callback)
{
	/// <summary>
	///     Appends the advanced find frame to the specified field using the DOM.<br />
	///     It also adds entity auto-complete to the field, and an event to detect changes<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	/// <param name="fieldName" type="String">The logical name of the field to replace.</param>
	/// <param name="entityNameFieldName" type="String">The logical name of the entity name field.</param>
	/// <param name="height" type="Number">[OPTIONAL=200]The height of the advanced find frame.</param>
	if (!fieldName || !entityNameFieldName)
	{
		var message = 'LoadAutoAdvancedFind: Parameter values are not set correctly.';
		console.error(message);
		throw message;
	}

	SetupEntityNameAutoComplete(entityNameFieldName, 10, function(resultEnforceValues)
	{
		var handler = function()
		{
			var entityName = GetFieldValue(entityNameFieldName);

			if (entityName && Contains(resultEnforceValues, null, entityName))
			{
				LoadAdvancedFind(fieldName, null, height || 200, entityNameFieldName);

				if (callback)
				{
					callback(true);
				}
			}
			else
			{
				RemoveAdvancedFind(fieldName);

				if (callback)
				{
					callback(false);
				}
			}
		};

		AddOnChange(entityNameFieldName, handler);

		handler();
	});
}

var MultiSelectPool = window.MultiSelectPool || {};

function LoadMultiSelect(fieldName, options, title, height, callback, isSingleMode, isSkipSort, isKeepVisible,
		isDisableFlowUi, isVisualiseIndent)
	{
		/// <summary>
		///     Appends the multi-select frame to the specified field using the DOM.<br />
		///     Author: Ahmed Elsawalhy
		/// </summary>
		/// <param name="fieldName" type="String" optional="false">
		///     The logical name of the field to replace.<br />
		///     It accepts the format 'fieldName:controlSuffix',
		///     which can be used to add the control to multiple field with the same name on the form.
		/// </param>
		/// <param name="options" type="object" optional="false">
		///     The array of values to display in the form of objects with 'text' and 'value'.
		///     OR
		///     the name of the option-set field to load values from
		///     OR
		///     an object containing properties:
		///     'entity', which is the name of the related entity;
		///     'relation', which is the name of the relationship to load;
		///     'field', which is the name of the field containing the value to display on the checkboxes
		/// </param>
		/// <param name="title" type="String" optional="false">The title to display in the frame.</param>
		/// <param name="height" type="Number" optional="true">The height of the advanced find frame.</param>
		/// <param name="isSingleMode" type="bool" optional="true">If 'true', will allow only single selection.</param>
		/// <param name="isSkipSort" type="bool" optional="true">If 'true', will skip sorting the selection entries.</param>
		/// <param name="isKeepVisible" type="bool" optional="true">
		///     If 'true', will keep the bound field visible after loading the
		///     control.
		/// </param>
		/// <param name="isDisableFlowUi" type="bool" optional="true">If 'false', will rearrange the items to fill the width.</param>
		/// <param name="isVisualiseIndent" type="bool" optional="true">If 'true', will show indentation using '|_' strings for each level.</param>
		if (!fieldName || !options || !title)
		{
			var message = 'Parameter values are not set correctly.';
			console.error(message);

			if (callback)
			{
				callback();
			}

			throw message;
		}

		if (typeof options === "string" && !Xrm.Page.getAttribute(options))
		{
			var message = "Couldn't load options from field: '" + options + "'";
			console.error(message);

			if (callback)
			{
				callback();
			}

			throw message;
		}

		var split = fieldName.split(':');
		fieldName = split[0];
		var controlSuffix = split[1];
		var controlName = fieldName + (controlSuffix || '');

		RemoveMultiSelect(controlName);

		var fieldContainer = GetInsertionRow(controlName);

		if (!fieldContainer)
		{
			return;
		}

		if (typeof parent !== "undefined")
		{
			parent.MultiSelectPool = MultiSelectPool;
			parent.IsUnique = IsUnique;
			parent.Remove = Remove;
			parent.Repeat = Repeat;
		}

		// the function to build the control
		var buildControl = function()
		{
			MultiSelectPool[fieldName] = options;

			if ($('#multiSelectRow_' + controlName, parent.document).length > 0
				|| $('#multiSelectRow_' + controlName).length > 0)
			{
				return;
			}

			var iFrameUrl = Xrm.Page.context.getClientUrl() + '/WebResources/ldv_MultiSelectHtml#' + fieldName +
				'#' + encodeURI(title) + '#' + (height || 200) + '#' + (isSingleMode === true) + '#' + (isSkipSort === true) +
				'#' + (isDisableFlowUi === true) + '#' + (isVisualiseIndent === true);

			// insert editor frame right after the field as a new row
			fieldContainer.before('<tr id="multiSelectRow_' + controlName + '"><td colspan="2">' +
				// auto size iFrame
				'<script type="text/javascript">' +
				'function AutoSizeMultiSelect(controlName) {' +
				// make sure there is something to resize
				'if (!document.getElementById("multiSelectFrame_' + controlName + '")' +
				' || !document.getElementById("multiSelectFrame_' + controlName + '").contentWindow' +
				' || !document.getElementById("multiSelectFrame_' + controlName + '").contentWindow.IsFrameLoaded) {' +
				'setTimeout(function () { AutoSizeMultiSelect(controlName); }, 200);' +
				'return;' +
				'}' +
				// resize frame to the editor size + 5 for padding
				'setTimeout(function() { $("#multiSelectFrame_" + controlName).height($("body",' +
				' $("#multiSelectFrame_" + controlName).contents()).height() + 17); }, 1000);' +
				'}' +
				// add an event for window resize
				'$(window).resize(function () { AutoSizeMultiSelect("' + controlName + '"); });' +
				'</script>' +
				// add editor to frame
				'<iframe id="multiSelectFrame_' + controlName +
				'" onload="AutoSizeMultiSelect(\'' + controlName + '\')" src="' +
				iFrameUrl +
				'" frameborder="0" scrolling="no"></iframe>' +
				'</td></tr>');

			var hide = function()
			{
				if (!isKeepVisible)
				{
					var fieldContainer = GetFieldContainer(controlName);

					if (fieldContainer == null)
					{
						return;
					}

					var found = false;
					var trs = $('tr', fieldContainer.parent());

					for (var i = 0; i < trs.length; i++)
					{
						if (trs[i].innerHTML === fieldContainer[0].innerHTML)
						{
							$(trs[i]).hide();
							found = true;
							continue;
						}

						if (found && !trs[i].innerHTML)
						{
							$(trs[i]).hide();
						}

						if (found && trs[i].innerHTML)
						{
							break;
						}
					}
				}

				if (callback)
				{
					setTimeout(callback, 500);
				}
			};

			hide();
		};

		// process relationship if passed as param
		if (options.relation)
		{
			if (typeof Sdk === "undefined")
			{
				LoadWebResources('ldv_sdksoapjs', function()
				{
					LoadMultiSelect(fieldName + controlSuffix, options, title, height, callback);
				});

				return;
			}

			var entityName = GetEntityName();
			var relatedEntityName = options.entity;
			var field = options.field;

			// build query
			var relatedQuery = new Sdk.Query.QueryExpression(relatedEntityName);
			relatedQuery.addColumn(relatedEntityName + 'id');
			relatedQuery.addColumn(field);

			var rtq = new Sdk.RelationshipQuery(options.relation, relatedQuery);
			var rqc = new Sdk.RelationshipQueryCollection();
			rqc.add(rtq);

			var req = new Sdk.RetrieveRequest(
				new Sdk.EntityReference(entityName, GetRecordId().replace('{', '').replace('}', '')),
				new Sdk.ColumnSet(entityName + 'id'),
				rqc);

			Sdk.Async.execute(req, function(result)
				{
					var related = result.getEntity()
						.getRelatedEntities().getRelatedEntitiesByRelationshipName(options.relation).getEntities();

					// reset options
					options = [];

					// build 'options' for multi-select control
					related.forEach(function(relatedEntity)
					{
						options.push(
							{
								text: relatedEntity.getValue(field),
								value: relatedEntity.getValue(relatedEntityName + 'id')
							});
					});

					// build the control
					buildControl();
				}, function(error)
				{
					var message = "Couldn't load related entities for multi-select: '" + error + "'";
					console.error(message);
					throw message;
				});
		}
		else
		{
			setTimeout(buildControl, 200);
		}
	}

function IsMultiSelectLoaded(fieldName)
{
	/// <summary>
	///     Checks whether the multi-select frame is loaded on the given field or not.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	var split = fieldName.split(':');
	fieldName = split[0];
	var controlSuffix = split[1];
	var controlName = fieldName + (controlSuffix || '');

	var oldFrame = $('#multiSelectFrame_' + controlName, parent.document);

	// CRM 2015-
	if (oldFrame.length <= 0)
	{
		oldFrame = $('#multiSelectFrame_' + controlName);
	}

	return oldFrame.length > 0;
}

function RemoveMultiSelect(fieldName, callback, isKeepHidden)
{
	/// <summary>
	///     Removes the frame from the form by deleting its DOM elements.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	var split = fieldName.split(':');
	fieldName = split[0];
	var controlSuffix = split[1];
	var controlName = fieldName + (controlSuffix || '');

	var oldFrameRow = $('#multiSelectRow_' + controlName, parent.document);

	// CRM 2015-
	if (oldFrameRow.length <= 0)
	{
		oldFrameRow = $('#multiSelectRow_' + controlName);
	}

	if (oldFrameRow.length > 0)
	{
		oldFrameRow.remove();
		delete MultiSelectPool[fieldName];
	}

	var show = function()
	{
		if (!isKeepHidden)
		{
			var fieldContainer = GetFieldContainer(controlName);

			if (fieldContainer == null)
			{
				return;
			}

			var found = false;
			var trs = $('tr', fieldContainer.parent());

			for (var i = 0; i < trs.length; i++)
			{
				if (trs[i].innerHTML === fieldContainer[0].innerHTML)
				{
					$(trs[i]).show();
					found = true;
					continue;
				}

				if (found && !trs[i].innerHTML)
				{
					$(trs[i]).show();
				}

				if (found && trs[i].innerHTML)
				{
					break;
				}
			}
		}

		if (callback)
		{
			setTimeout(callback, 500);
		}
	};

	show();
}

function GetFieldContainer(fieldName)
{
	/// <summary>
	/// Internal use only!
	/// </summary>
	// get field table row to insert frame after it later
	var field = $('#' + fieldName + '_d');

	// CRM 2016+
	if (field.length <= 0)
	{
		field = $('#' + fieldName + '_d', parent.document);
	}

	var fieldContainer = field.parent();

	if (fieldContainer.length <= 0)
	{
		return null;
	}

	while (!fieldContainer.is('tr'))
	{
		fieldContainer = fieldContainer.parent();
	}

	return fieldContainer;
}

function GetInsertionRow(fieldName)
{
	/// <summary>
	/// Internal use only!
	/// </summary>
	var fieldContainer = GetFieldContainer(fieldName);

	if (fieldContainer == null)
	{
		console.error("Error while creating frame: can't find field with name '" + fieldName + "'.");
		return null;
	}

	var found = false;
	var set = false;
	var trs = $('tr', fieldContainer.parent());

	trs.each(function(tr)
	{
		if (set)
		{
			return;
		}

		if (trs[tr].innerHTML === fieldContainer[0].innerHTML)
		{
			found = true;
			return;
		}

		if (found && trs[tr].innerHTML)
		{
			fieldContainer = $(trs[tr]);
			set = true;
		}
	});

	return fieldContainer;
}

var BusyIndicatorMessageStack = window.BusyIndicatorMessageStack || [];

function ShowBusyIndicator(text, id)
{
	/// <summary>
	///     Shows a busy frame at the top right of the screen that fits its text.<br />
	///     Author: Ahmed Elsawalhy<br />
	///     credits: http://andreaswijayablog.blogspot.pt/2011/09/crm-2011-ajax-loading-message-screen.html
	/// </summary>
	/// <param name="text" type="String" mayBeNull="false" optional="false">
	///     The text to show in the frame.
	/// </param>
	/// <param name="id" type="String" mayBeNull="true" optional="true">An optional ID.</param>
	try
	{
		var $ = window.$;

		// Try get the form header
		var header = $("#formHeaderContainer");

		if (header.length <= 0)
		{
			header = $("#gridControlBar");
		}

		if (header.length <= 0)
		{
			$ = parent.$;

			// Try get the form header again (2015 SP1)
			header = $("#formHeaderContainer");
		}

		var setBusyIndicatorText = function()
		{
			try
			{
				// if there isn't a busy frame, initialise first.
				if ($('#loadingDiv').length <= 0)
				{
					ShowBusyIndicator(text, id);
					return;
				}

				$('#loadingText').text(text);
				$('#loadingDiv').center();
				$('#loadingDiv').css('width', $('#loadingText').textWidth() + 'px');
			}
			catch (e)
			{
			}
		};
		BusyIndicatorMessageStack.push({ id: id || text, text: text });

		// if there is a busy frame already, change its text.
		if ($('#loadingDiv').length)
		{
			setBusyIndicatorText(text);
			return;
		}

		// a function added to jQuery to center an HTML element in a window.
		$.fn.center = function()
		{
			this.css("position", "absolute");
			this.css("top", '10px');
			this.css("left", (($(top).width() - $('#loadingText').textWidth() - 40) + 'px'));
			return this;
		};

		$.fn.textWidth = function()
		{
			var htmlOrg = $(this).html();
			var htmlCalc = '<span>' + htmlOrg + '</span>';
			$(this).html(htmlCalc);
			var width = $(this).find('span:first').width();
			$(this).html(htmlOrg);
			return Math.max(50, width);
		};

		// add the loading element to the page's HTML
		$(header).append('<div id="loadingDiv"></div>');
		var loadingDiv = $('#loadingDiv');

		// add icon to div with its CSS
		$('<p id="loadingIcon"></p>')
			.appendTo(loadingDiv)
			.css('height', '50px')
			.css('background', 'url(/_imgs/AdvFind/progress.gif) no-repeat')
			.css('background-position', '50% 50%');
		// add text to div with its CSS
		$('<p id="loadingText">' + text + '</p>')
			.appendTo(loadingDiv)
			.css('padding', '0 2px 2px 2px')
			.css('text-align', 'center')
			.css('color', '#444444')
			.css('font-size', '13px')
			.css('font-family', 'Segoe UI, Tahoma, Arial');
		// set shadow, colour, ...etc. for div
		loadingDiv
			.css('box-shadow', '5px 5px 8px #aaa')
			.css('background-color', '#ffffff')
			.css('border', '1px black solid')
			.css('width', '300px')
			.css('z-index', '99999') // top most
			.center()
			.hide(); // hide it initially 

		// show the loading element
		$('#loadingDiv').show();

		setBusyIndicatorText(text);
	}
	catch (e)
	{
	}
}

function HideBusyIndicator(id)
{
	/// <summary>
	///     Hides the busy indicator. If an 'ID' is not given, the latest added one will be hidden.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	try
	{
		if (BusyIndicatorMessageStack.length <= 0)
		{
			return;
		}

		var $ = window.$;

		// Try get the form header
		var header = $("#formHeaderContainer");

		if (header.length <= 0)
		{
			header = $("#gridControlBar");
		}

		if (header.length <= 0)
		{
			$ = parent.$;
		}

		if (!id)
		{
			// remove the lastest message
			BusyIndicatorMessageStack.pop();
		}
		else
		{
			var objs = Search(BusyIndicatorMessageStack, 'id', id, 1, true);

			// skip if it doesn't exist
			if (!objs)
			{
				return;
			}

			Remove(BusyIndicatorMessageStack, null, objs[0], 1);
		}

		// if more messages, then display the current lastest one
		if (BusyIndicatorMessageStack.length)
		{
			var message = BusyIndicatorMessageStack.pop();
			ShowBusyIndicator(message.text, message.id);
		}
		else
		{
			// if no more messages, remove the indicator
			$('#loadingDiv').remove();
		}
	}
	catch (e)
	{
	}
}

function ShowLoadingContainer(messageTitle, isTransparent, isModal, isHideRibbon, opacity)
{
	/// <summary>
	/// Uses CRM's loading screen to show a message.<br />
	///     Author: Tarek Selem
	/// </summary>
	isTransparent = isTransparent || false;
	isModal = isModal || false;
	isHideRibbon = isHideRibbon || false;

	//Hide Ribbon
	if (isHideRibbon)
	{
		var ribbon = $("div#crmRibbonManager", this.parent.document);
		if (ribbon)
		{
			ribbon.css('display', 'none');
			ribbon.css('visibility', 'hidden');
		}
	};

	//Show Loading Model 
	if (isModal)
	{
		var loadingDivDialog = $("#processingDialog");

		if (loadingDivDialog.length > 0)
		{
			// Show Loader
			loadingDivDialog.css('display', 'inline');

			//Set background Transparent
			if (isTransparent === true)
			{
				loadingDivDialog.css('opacity', opacity || '0.2');
			}
		}

		//Display Message
		if (messageTitle)
		{
			$("#processingDialog > div > span").text(messageTitle);
		}
	}
	else
	{ // Show Loading Page
		var loadingDivContainer = $("div#containerLoadingProgress");
		var loadingDivTitle = $("#loadingtext");

		if (loadingDivContainer)
		{
			loadingDivContainer.css('display', 'inline');

			if (loadingDivTitle && messageTitle)
			{
				loadingDivTitle.text(messageTitle);
			}

			//Show Message for 2015
			if (loadingDivTitle.length === 0 && messageTitle)
			{
				loadingDivContainer.append("<br/><p><font size='4'>" + messageTitle + "</font></p>");
				//loadingDivTitle.text(messageTitle);
			}

			if (isTransparent === true)
			{
				loadingDivContainer.css('opacity', opacity || '0.2');
			}
		}
	}
}

function HideLoadingContainer()
{
	/// <summary>
	/// Hides CRM's loading screen.<br />
	///     Author: Tarek Selem
	/// </summary>
	var ribbon = $("div#crmRibbonManager", this.parent.document);
	if (ribbon)
	{
		ribbon.css('display', 'block');
		ribbon.css('visibility', 'visible');
	}

	var loadingDivDialog = $("#processingDialog");

	if (loadingDivDialog)
	{
		loadingDivDialog.css('display', 'none');
	}

	var loadingDivContainer = $("#containerLoadingProgress");
	loadingDivContainer.css('display', 'none');
	$("div#containerLoadingProgress > p").remove();
	$("div#loadingtext > p").remove();
}

//#endregion

///////////////////////////////////////////////
//#region >>>>>>>>> Form control <<<<<<<<<<< //

//#region >>>>>>>>> Validations <<<<<<<<<<< //

function ValidateDateFieldInFuture(fieldName, isResetDateOnError, dateErrorMsg)
{
	/// <summary>
	/// Validates that the field's date value is in the future. The present moment is counted as well.
	/// A custom error message can be displayed instead of the default one.<br />
	///     Author: Ahmed elSawalhy
	/// </summary>

	// get dates from the form
	var startTime = Xrm.Page.getAttribute(fieldName).getValue();

	// make sure that the expected time is in the future
	if (startTime && CompareDate(startTime, new Date()) > 0)
	{
		if (isResetDateOnError)
		{
			Xrm.Page.getAttribute(fieldName).setValue();
		}

		ShowControlError(fieldName, dateErrorMsg ||
									GetUserLanguageCode() === Language.ENGLISH
									? 'Date must be in the future.'
									: 'يجب ان يكون التاريخ فى المستقبل.');
	}
	else
	{
		ClearControlError(fieldName);
	}
}

function ValidateDateFieldsOrder(initialFieldName, nextFieldName, isResetEndDateOnError,
		startDateMissingErrorMsg, endDateBeforeStartErrorMsg)
	{
	/// <summary>
	/// Validates that the date value of two fields either equal or are sequential.
	/// A custom error message can be displayed instead of the default one.<br />
	///     Author: Ahmed elSawalhy
	/// </summary>

	// get dates from the form
		var startTime = Xrm.Page.getAttribute(initialFieldName).getValue();
		var endTime = Xrm.Page.getAttribute(nextFieldName).getValue();

		if (!startTime && !endTime)
		{
			ClearControlError(nextFieldName);
			return;
		}

		// make sure that the start time exists
		if (!startTime)
		{
			if (isResetEndDateOnError)
			{
				Xrm.Page.getAttribute(nextFieldName).setValue();
			}

			ShowControlError(nextFieldName, startDateMissingErrorMsg ||
											GetUserLanguageCode() === Language.ENGLISH
											? 'Start date must be set first.'
											: 'يجب ادخال قيمة "تاريخ البدء" اولا.');
		}
		// make sure that the end time is after the start time
		else if (endTime && CompareDate(endTime, startTime) > 0)
		{
			if (isResetEndDateOnError)
			{
				Xrm.Page.getAttribute(nextFieldName).setValue();
			}

			ShowControlError(nextFieldName, endDateBeforeStartErrorMsg ||
											GetUserLanguageCode() === Language.ENGLISH
											? 'End date value must be after start date.'
											: 'يجب ان يكون "تاريخ البدء" قبل "تاريخ الانتهاء".');
		}
		else
		{
			ClearControlError(nextFieldName);
		}
	}

function ValidateArabicCharacters(fieldname)
{
	Xrm.Page.getControl(fieldname).clearNotification();

	var e = Xrm.Page.getAttribute(fieldname).getValue();

	if (e)
	{
		e = e.replace(/\s/g, '').trim().replace(" ", "");

		for (var i = 0; i < e.length; i++)
		{
			var unicode = e.charCodeAt(i);

			if ((unicode < 0x0600 || unicode > 0x06FF) || (unicode >= 0xFE70 && unicode <= 0xFEFF))
			{
				var message = "You must enter a valid arabic letters.";
				Xrm.Page.getControl(fieldname).setNotification(message);
				Xrm.Page.getControl(fieldname).setFocus();
				return false;
			}
		}
	}

	return true;
}


function ValidateEnglishCharacters(fieldname)
{
	Xrm.Page.getControl(fieldname).clearNotification();

	if (Xrm.Page.getAttribute(fieldname).getValue() == null)
	{
		return true;
	}
	else
	{
		var english = /^[A-Za-z0-9]*$/;
		var fld = Xrm.Page.getAttribute(fieldname).getValue().replace(/\s/g, '');

		if (fld != null && !english.test(fld))
		{
			var message = "You must enter a valid English letters.";
			Xrm.Page.getControl(fieldname).setNotification(message);
			Xrm.Page.getControl(fieldname).setFocus();
			return false;
		}
		else
		{
			Xrm.Page.getControl(fieldname).clearNotification();
		}

		return true;
	}
}

//#endregion

//#region >>>>>>>>> Notifications <<<<<<<<<<< //

NotificationLevel =
	{
		INFORMATION: 'INFORMATION',
		WARNING: 'WARNING',
		ERROR: 'ERROR'
	};

var FormNotifications = window.FormNotifications || [];

function SetFormNotification(englishMessage, arabicMessage, level, id)
{
	/// <summary>
	/// Shows the notification message of CRM on top of the form.
	/// Use the 'NotificationLevel' object to pass the notification level.<br />
	///     Author: Ahmed elSawalhy
	/// </summary>
	var language = Xrm.Page.context.getUserLcid();

	ClearFormNotification(id);

	if (language === 1025)
	{
		Xrm.Page.ui.setFormNotification(arabicMessage || englishMessage, level, id);
	}
	else
	{
		Xrm.Page.ui.setFormNotification(englishMessage || arabicMessage, level, id);
	}

	FormNotifications.push(id);
}

function ClearFormNotification(id)
{
	/// <summary>
	/// Hides CRM's form notification.<br />
	///     Author: Ahmed elSawalhy
	/// </summary>
	var index = FormNotifications.indexOf(id);

	if (index < 0)
	{
		return;
	}

	Xrm.Page.ui.clearFormNotification(id);
	FormNotifications.splice(index, 1);
}

function ClearFormNotifications()
{
	/// <summary>
	/// Hides all CRM's form notifications.<br />
	///     Author: Ahmed elSawalhy
	/// </summary>
	for (var i = 0; i < FormNotifications.length; i++)
	{
		ClearFormNotification(FormNotifications[i]);
	}
}

function ShowControlError(controlName, message)
{
	Xrm.Page.getControl(controlName).setNotification(message);
}

function ClearControlError(controlName)
{
	Xrm.Page.getControl(controlName).clearNotification();
}

//#endregion

//#region >>>>>>>>> Form <<<<<<<<<<< //

var FormType = {
		Undefined: 0,
		Create: 1,
		Update: 2,
		ReadOnly: 3,
		Disabled: 4,
		QuickCreate: 5,
		BulkEdit: 6,
		ReadOptimized: 11
	};

function GetFormType()
{
	/// <summary>
	///     Returns the form type. 'FormType' global object can be used to compare values.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	return Xrm.Page.ui.getFormType();
}

function SetFormLocked(locked, exceptions)
{
	/// <summary>
	///     Loops over all fields on the form, and locks them.<br />
	/// credit: http://crmexplorer.com/2011/11/disable-wnable-fields-sections-tabs-and-the-whole-form-in-crm-2011/ <br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	var attributes = Xrm.Page.data.entity.attributes.get();

	for (var i in attributes)
	{
		if (attributes.hasOwnProperty(i))
		{
			var attribute = Xrm.Page.data.entity.attributes.get(attributes[i].getName());
			var name = attribute.getName();

			try
			{
				if (!exceptions || (exceptions.length > 0 && exceptions.indexOf(name) < 0))
				{
					SetFieldLocked(name, locked);
				}
			}
			catch (e)
			{
			}
		}
	}
}

function IsFormDirty()
{
	/// <summary>
	///     Checks all fields on the form and returns true if a field has an unsaved value.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	/// <returns type="bool">'true' if there is an unsaved value.</returns>
	return Xrm.Page.data.entity.getIsDirty();
}

var SaveMode = {
		Default: null,
		SaveAndClose: 'saveandclose',
		SaveAndNew: 'saveandnew'
	};

function SaveForm(saveMode)
{
	/// <summary>
	///     Saves the form using the given save mode. Accepts 'SaveMode' type.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	/// <param name="saveMode" type="SaveMode" optional="true">
	///     [OPTIONAL=SaveMode.Default]Save mode to set.
	///     Accepts 'SaveMode' type.
	/// </param>
	Xrm.Page.data.entity.save(saveMode);
}

function GetFormTabNames()
{
	/// <summary>
	///     Gets the names of the tabs on the form.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	/// <param name="tabname" type="String" optional="false">Tab name.</param>
	/// <param name="required" type="bool" optional="false">'true' to set fields as required.</param>
	var tabs = Xrm.Page.ui.tabs.get();
	var tabNames = [];

	if (tabs != null)
	{
		tabs.forEach(function(tab)
		{
			tabNames.push(tab.getName());
		});
	}

	return tabNames;
}

function GetFormFieldNames()
{
	/// <summary>
	///     Returns all form field names as an array.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	var attributes = Xrm.Page.data.entity.attributes.get();
	var attributeNames = [];

	for (var i in attributes)
	{
		if (attributes.hasOwnProperty(i))
		{
			attributeNames.push(attributes[i].getName());
		}
	}

	return attributeNames;
}

function GetFormId()
{
	return Xrm.Page.ui.formSelector.getCurrentItem().getId();
}

function RefreshFormData()
{
	Xrm.Page.data.refresh();
}

//#endregion

//#region >>>>>>>>> Tabs <<<<<<<<<<< //

function SetTabLabel(tabName, label)
{
	Xrm.Page.ui.tabs.get(tabName).setLabel(label);
}

function GetTabSectionNames(tabname)
{
	/// <summary>
	///     Takes a tab name/number and gets the names of the sections inside.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	/// <param name="tabname" type="String" optional="false">Tab name.</param>
	var tab = Xrm.Page.ui.tabs.get(tabname);
	var sectionNames = [];

	if (tabname != null && tab != null)
	{
		tab.sections.get().forEach(function(section)
		{
			sectionNames.push(section.getName());
		});
	}

	return sectionNames;
}

function GetTab(tabName)
{
	return Xrm.Page.ui.tabs.get(tabName);
}

function SetTabVisible(tabName, visible)
{
	GetTab(tabName).setVisible(visible);
}

function SetTabsVisible(tabs, visible)
{
	/// <summary>
	///     Takes an array of tab names/numbers and calls "SetTabVisible" on each.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	/// <param name="tabs" type="String[]" optional="false">An array of tab names.</param>
	/// <param name="visible" type="bool" optional="false">'true' to show the tabs.</param>
	for (var i = 0; i < tabs.length; i++)
	{
		SetTabVisible(tabs[i], visible);
	}
}

function IsTabExpanded(tabName)
{
	return GetTab(tabName).getDisplayState() === 'expanded';
}

function SetTabRequired(tabname, required)
{
	/// <summary>
	///     Takes a tab name/number and calls "SetFieldRequired" on each field in the tab.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	/// <param name="tabname" type="String" optional="false">Tab name.</param>
	/// <param name="required" type="bool" optional="false">'true' to set fields as required.</param>
	var tab = Xrm.Page.ui.tabs.get(tabname);
	if (tabname != null && tab != null)
	{
		tab.sections.get().forEach(function(section)
		{
			var controls = section.controls.get();
			for (var i in controls)
			{
				if (controls.hasOwnProperty(i))
				{
					var control = controls[i];
					if (typeof control.getAttribute !== 'undefined')
					{
						SetFieldRequired(control.getAttribute().getName(), required);
					}
				}
			}
		});
	}
}

function SetTabsRequired(tabs, required)
{
	/// <summary>
	///     Takes an array of tab names/numbers and calls "SetTabRequired" on each.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	/// <param name="tabs" type="String[]" optional="false">An array of tab names.</param>
	/// <param name="visible" type="bool" optional="false">'true' to set tabs as required.</param>
	for (var i = 0; i < tabs.length; i++)
	{
		SetTabRequired(tabs[i], required);
	}
}

function SetTabLocked(tabname, locked)
{
	/// <summary>
	///     Takes a tab name/number and calls "SetFieldLocked" on each field in the tab.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	/// <param name="tabname" type="String" optional="false">Tab name.</param>
	/// <param name="locked" type="bool" optional="false">'true' to lock fields.</param>
	var tab = Xrm.Page.ui.tabs.get(tabname);
	if (tabname != null && tab != null)
	{
		tab.sections.get().forEach(function(section)
		{
			var controls = section.controls.get();
			for (var i in controls)
			{
				if (controls.hasOwnProperty(i))
				{
					var control = controls[i];
					if (typeof control.getAttribute !== 'undefined')
					{
						SetFieldLocked(control.getAttribute().getName(), locked);
					}
				}
			}
		});
	}
}

function SetTabsLocked(tabs, locked)
{
	/// <summary>
	///     Takes an array of tab names/numbers and calls "SetTabLocked" on each.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	/// <param name="tabs" type="String[]" optional="false">An array of tab names.</param>
	/// <param name="visible" type="bool" optional="false">'true' to lock tabs.</param>
	for (var i = 0; i < tabs.length; i++)
	{
		SetTabLocked(tabs[i], locked);
	}
}

//#endregion

//#region >>>>>>>>> Sections <<<<<<<<<<< //

function GetSectionFieldNames(tabname, sectionName)
{
	/// <summary>
	///     Takes a tab name/number and gets the names of the fields inside.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	/// <param name="tabname" type="String" optional="false">Tab name.</param>
	/// <param name="sectionName" type="String" optional="false">Section name.</param>
	var tab = Xrm.Page.ui.tabs.get(tabname);
	var attributeNames = [];

	if (tabname != null && tab != null)
	{
		var section = tab.sections.get(sectionName);
		if (section != null)
		{
			var controls = section.controls.get();
			for (var i in controls)
			{
				if (controls.hasOwnProperty(i))
				{
					var control = controls[i];
					if (typeof control.getAttribute !== 'undefined')
					{
						attributeNames.push(control.getAttribute().getName());
					}
				}
			}
		}
	}

	return attributeNames;
}

function GetSection(tabNumber, sectionName)
{
	var tab = GetTab(tabNumber);
	return tab ? tab.sections.get(sectionName) : null;
}

function SetSectionVisible(tabNumber, sectionName, visible)
{
	GetSection(tabNumber, sectionName).setVisible(visible);
}

function SetSectionsVisible(sections, visible)
{
	/// <summary>
	///     Takes an array of section names and calls "SetSectionVisible" on each.<br />
	///     The section names must be in the format "[tab]:[section]".<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	/// <param name="sections" type="String[]" optional="false">An array of section names.</param>
	/// <param name="visible" type="bool" optional="false">'true' to show the sections.</param>
	for (var i = 0; i < sections.length; i++)
	{
		var splitTabAndSection = sections[i].split(':');
		SetSectionVisible(splitTabAndSection[0], splitTabAndSection[1], visible);
	}
}

function SetSectionRequired(tabname, sectionName, required)
{
	/// <summary>
	///     Takes a section name and calls "SetFieldRequired" on each field in the section.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	/// <param name="tabname" type="String" optional="false">Tab name.</param>
	/// <param name="sectionName" type="String" optional="false">Section name.</param>
	/// <param name="required" type="bool" optional="false">'true' to set fields as required.</param>
	var tab = Xrm.Page.ui.tabs.get(tabname);
	if (tabname != null && tab != null)
	{
		var section = tab.sections.get(sectionName);
		if (section != null)
		{
			var controls = section.controls.get();
			for (var i in controls)
			{
				if (controls.hasOwnProperty(i))
				{
					var control = controls[i];
					if (typeof control.getAttribute !== 'undefined')
					{
						SetFieldRequired(control.getAttribute().getName(), required);
					}
				}
			}
		}
	}
}

function SetSectionsRequired(sections, required)
{
	/// <summary>
	///     Takes a section array and calls "SetSectionRequired" on each section.<br />
	///     The section names must be in the format "[tab]:[section]".<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	/// <param name="sections" type="String[]" optional="false">An array of section names.</param>
	/// <param name="visible" type="bool" optional="false">'true' to set sections as required.</param>
	for (var i = 0; i < sections.length; i++)
	{
		var splitTabAndSection = sections[i].split(':');
		SetSectionRequired(splitTabAndSection[0], splitTabAndSection[1], required);
	}
}

function SetSectionLocked(tabname, sectionName, locked)
{
	/// <summary>
	///     Takes a tab name/number and calls "SetFieldLocked" on each field in the section.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	/// <param name="tabname" type="String" optional="false">Tab name.</param>
	/// <param name="sectionName" type="String" optional="false">Section name.</param>
	/// <param name="locked" type="bool" optional="false">'true' to lock fields.</param>
	var tab = Xrm.Page.ui.tabs.get(tabname);
	if (tabname != null && tab != null)
	{
		var section = tab.sections.get(sectionName);
		if (section != null)
		{
			var controls = section.controls.get();
			for (var i in controls)
			{
				if (controls.hasOwnProperty(i))
				{
					var control = controls[i];
					if (typeof control.getAttribute !== 'undefined')
					{
						SetFieldLocked(control.getAttribute().getName(), locked);
					}
				}
			}
		}
	}
}

function SetSectionsLocked(sections, locked)
{
	/// <summary>
	///     Takes a section array and calls "SetSectionLocked" on each section.<br />
	///     The section names must be in the format "[tab]:[section]".<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	/// <param name="sections" type="String[]" optional="false">An array of section names.</param>
	/// <param name="locked" type="bool" optional="false">'true' to set sections as locked.</param>
	for (var i = 0; i < sections.length; i++)
	{
		var splitTabAndSection = sections[i].split(':');
		SetSectionLocked(splitTabAndSection[0], splitTabAndSection[1], locked);
	}
}

//#endregion

//#region >>>>>>>>> Fields <<<<<<<<<<< //

function GetField(name)
{
	return Xrm.Page.getAttribute(name);
}

function GetFieldFromContext(context)
{
	return context.getEventSource();
}

function FieldFireOnChange(name)
{
	GetField(name).fireOnChange();
}

function GetFieldValue(name)
{
	return GetField(name).getValue();
}

function SetFieldValue(name, value, fireOnChange)
{
	/// <summary>
	///     Sets the value in the field given, and fires the 'OnChange' event if required.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	GetField(name).setValue(value);

	if (fireOnChange)
	{
		GetField(name).fireOnChange();
	}
}

function GetLookupValue(name)
{
	return GetFieldValue(name)[0];
}

function SetLookupValue(name, id, text, entityLogicalName)
{
	var value =
		[{
				id: id,
				name: text,
				entityType: entityLogicalName
			}];

	Xrm.Page.getAttribute(name).setValue(value);
}

function ClearFieldValue(name, fireOnChange)
{
	/// <summary>
	///     Clears the value from the field given, and fires the 'OnChange' event if required.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	GetField(name).setValue(null);

	if (fireOnChange)
	{
		GetField(name).fireOnChange();
	}
}

var SubmitMode = {
		Always: 'always',
		Never: 'never',
		Dirty: 'dirty'
	};

function SetFieldSubmitMode(fieldName, submitMode)
{
	/// <summary>
	///     Set the submit mode OnSave of the field. Accepts 'SubmitMode' type.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	/// <param name="submitMode" type="SubmitMode" optional="true">
	///     [OPTIONAL=SubmitMode.Dirty]Submit mode to set.
	///     Accepts 'SubmitMode' type.
	/// </param>
	submitMode = submitMode || SubmitMode.Dirty;
	Xrm.Page.getAttribute(fieldName).setSubmitMode(submitMode);
}

function IsFieldDirty(name)
{
	return GetField(name).getIsDirty();
}

function IsFieldVisible(name)
{
	return Xrm.Page.getControl(name).getVisible();
}

function IsFieldLocked(name)
{
	return Xrm.Page.getControl(name).getDisabled();
}

function IsFieldRequired(name)
{
	return Xrm.Page.getAttribute(name).getRequiredLevel() === "required";
}

function SetFieldsVisible(fields, visible)
{
	/// <summary>
	///     Takes an array of field names and calls "SetFieldVisible" on each.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	/// <param name="fields" type="String[]" optional="false">An array of field names.</param>
	/// <param name="visible" type="bool" optional="false">'true' to show the fields.</param>
	for (var i = 0; i < fields.length; i++)
	{
		SetFieldVisible(fields[i], visible);
	}
}

function SetFieldsRequired(fields, required)
{
	/// <summary>
	///     Takes an array of field names and calls "SetFieldRequired" on each.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	/// <param name="fields" type="String[]" optional="false">An array of field names.</param>
	/// <param name="visible" type="bool" optional="false">'true' to set as required.</param>
	for (var i = 0; i < fields.length; i++)
	{
		SetFieldRequired(fields[i], required);
	}
}

function SetFieldsLocked(fields, locked)
{
	/// <summary>
	///     Takes an array of field names and calls "SetFieldLocked" on each.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	/// <param name="fields" type="String[]" optional="false">An array of field names.</param>
	/// <param name="locked" type="bool" optional="false">'true' to lock the fields.</param>
	for (var i = 0; i < fields.length; i++)
	{
		SetFieldLocked(fields[i], locked);
	}
}

function SetFieldVisible(name, visible)
{
	ApplyActionToField(name, Xrm.Page.getControl, 'setVisible', visible);
}

function SetFieldLocked(name, locked)
{
	ApplyActionToField(name, Xrm.Page.getControl, 'setDisabled', locked);
}

function SetFieldRequired(name, required)
{
	if (required)
	{
		ApplyActionToField(name, Xrm.Page.getAttribute, 'setRequiredLevel', 'required');
	}
	else
	{
		ApplyActionToField(name, Xrm.Page.getAttribute, 'setRequiredLevel', 'none');
	}
}

function ApplyActionToField(name, fetchFunction, action, value)
{
	/// <summary>
	/// Internal use only!
	/// </summary>
	var control;
	var index = 0;
	var indexedName = name;

	while (control = fetchFunction(indexedName))
    {
		control[action](value);
		indexedName = name + (++index);
	}
}

function UserRoleCanReadField(attribute)
{
	return attribute.getUserPrivilege().canRead;
}

function GetFieldsValues(fieldNames, isSkipNull)
{
	/// <summary>
	///     Accepts a CSV of field names or array, and returns the values in those fields as an array.
	/// Skips null values if required.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	var values = [];

	if (typeof fieldNames === "string")
	{
		fieldNames = fieldNames.split(',');
	}

	for (var i = 0; i < fieldNames.length; i++)
	{
		var value = GetFieldValue(fieldNames[i]);

		if (value || !isSkipNull)
		{
			values.push(value);
		}
	}

	return values;
}

//#endregion

//#region >>>>>>>>> OptionSets <<<<<<<<<<< //

function AddOptionSetValues(optionSetName, values, isClearFirst)
{
	/// <summary>
	///     Add the values given to the option-set. The values should be in the format
	///     '[{value: value, text: text}].
	/// </summary>
	var optionSet = Xrm.Page.ui.controls.get(optionSetName);

	if (optionSet != null)
	{
		if (isClearFirst)
		{
			ClearOptionSetValues(optionSetName);
		}

		for (var i = 0; i < values.length; i++)
		{
			optionSet.addOption(values[i]);
		}
	}
}

function ClearOptionSetValues(optionSetName)
{
	var optionSet = Xrm.Page.ui.controls.get(optionSetName);

	if (optionSet != null)
	{
		optionSet.clearOptions();
	}
}

function RemoveOptionSetValues(optionSetName, values)
{
	/// <summary>
	///     Removes the values given from the option-set.
	/// </summary>
	var optionSet = Xrm.Page.ui.controls.get(optionSetName);

	if (optionSet != null)
	{
		for (var i = 0; i < values.length; i++)
		{
			optionSet.removeOption(values[i]);
		}
	}
}

//#endregion

//#region >>>>>>>>> BPF <<<<<<<<<<< //

function GetSelectedStage()
{
	return Xrm.Page.data.process.getSelectedStage();
}

function GetActiveStage()
{
	return Xrm.Page.data.process.getActiveStage();
}

function SetHeaderFieldVisible(name, visible, checkCount, suffix)
{
	/// <summary>
	///     Set the visibility of the field in the BPF.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	var control = GetHeaderFieldControl(name + (suffix || ""));

	if (control)
	{
		control.setVisible(visible);
		SetHeaderFieldVisible(name, visible, checkCount, suffix ? ++suffix : 1);
	}
	else if ((suffix = (suffix ? ++suffix : 1)) <= (checkCount || 2))
	{
		SetHeaderFieldVisible(name, visible, checkCount, suffix);
	}
}

function SetHeaderFieldLocked(name, locked, checkCount, suffix)
{
	/// <summary>
	///     Set the lock state of the field in the BPF.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	var control = GetHeaderFieldControl(name + (suffix || ""));

	if (control)
	{
		control.setDisabled(locked);
		SetHeaderFieldLocked(name, locked, checkCount, suffix ? ++suffix : 1);
	}
	else if ((suffix = (suffix ? ++suffix : 1)) <= (checkCount || 2))
	{
		SetHeaderFieldLocked(name, locked, checkCount, suffix);
	}
}

function SetHeaderFieldRequired(name, required, checkCount, suffix)
{
	/// <summary>
	///     Set the required state of the field in the BPF.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	var control = GetHeaderFieldControl(name + (suffix || ""));

	if (control)
	{
		// multi-line fields can't reflect the required state using XRM
		if (GetField(name).getAttributeType() === "memo")
		{
			return;
		}

		if (required)
		{
			control.setRequiredLevel("required");
		}
		else
		{
			control.setRequiredLevel("none");
		}

		SetHeaderFieldRequired(name, required, checkCount, suffix ? ++suffix : 1);
	}
	else if ((suffix = (suffix ? ++suffix : 1)) <= (checkCount || 2))
	{
		SetHeaderFieldRequired(name, required, checkCount, suffix);
	}
}

function GetHeaderFieldControl(name)
{
	return Xrm.Page.getControl('header_process_' + name);
}

function SetBpfVisible(isVisible)
{
	Xrm.Page.ui.process.setVisible(isVisible);
}

//#endregion

//#region >>>>>>>>> Misc <<<<<<<<<<< //

function GetControl(name)
{
	return Xrm.Page.getControl(name);
}

function GetControlValue(name)
{
	return GetControl(name).getValue();
}

function SetSubgridLocked(gridName, isLocked)
{
	// updated to use 2016 function -- Sawalhy
	Xrm.Page.getControl(gridName).addOnLoad(function()
	{
		$('#' + gridName + '_addImageButton').css('display', isLocked ? 'none' : '');
		$('#' + gridName + '_openAssociatedGridViewImageButton').css('display', isLocked ? 'none' : '');

		var hideDelete = function(delay)
		{
			setTimeout(function()
			{
				if ($('#' + gridName + '_divDataArea').find('#GridLoadingMessage').length <= 0)
				{
					$('#' + gridName + '_gridBodyContainer .ms-crm-List-DeleteContainer')
						.css('display', isLocked ? 'none' : '');
				}
				else
				{
					hideDelete(500);
				}
			}, delay);
		};
		hideDelete(1500);
	});

	RefreshSubGrid(gridName);
}

function RefreshSubGrid(gridName)
{
	Xrm.Page.getControl(gridName).refresh();
}

//#endregion

//#region >>>>>>>>> Events <<<<<<<<<<< //

function AddOnChange(fieldName, handler)
{
	GetField(fieldName).addOnChange(handler);
}

function RemoveOnChange(fieldName, handler)
{
	GetField(fieldName).removeOnChange(handler);
}

function AddOnKeyPress(controlName, handler)
{
	GetControl(controlName).addOnKeyPress(handler);
}

function RemoveOnKeyPress(controlName, handler)
{
	GetControl(controlName).removeOnKeyPress(handler);
}

//#endregion

//#region >>>>>>>>> Auto-complete <<<<<<<<<<< //

var AutoCompleteKeyPressMap = window.AutoCompleteKeyPressMap || {};
var AutoCompleteOnChangeMap = window.AutoCompleteKeyPressMap || {};

function SetupAutoComplete(fieldName, result, resultEnforceValues, maxResults, isEnforceListValue, isSort, callback)
{
	/// <summary>
	///     Adds auto-complete to a field using the passed result list.<br />
	///     The result list should be in the form:
	///     {
	///     results:
	///     [{
	///     id: {{value1}},
	///     icon: {{url}},
	///     fields: [name: {{fieldValue1}}, name2: {{fieldValue2}}, name3: {{fieldValue3}}]
	///     }],
	///     commands:
	///     {
	///     id: {{value}},
	///     icon: {{url}},
	///     label: {{value}},
	///     action: {{function reference}}
	///     }
	///     }<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	/// <param name="fieldName" type="String" optional="false">Field name to add auto-complete to.</param>
	/// <param name="result" type="Object" optional="false">The result object to use for auto-complete list.</param>
	/// <param name="resultEnforceValues" type="String[]" optional="false">
	///     The result names list to compare when enforcing the
	///     list.
	/// </param>
	/// <param name="maxResults" type="Number" optional="true">The maximum number of entries to show in the drop-down.</param>
	/// <param name="isEnforceListValue" type="bool" optional="true">
	///     A flag to enforce only the values in the list, otherwise throw error.
	/// </param>
	/// <param name="isSort" type="bool" optional="true">'true' will sort the list.</param>
	if (fieldName === null || fieldName === undefined
		|| result === null || result === undefined
		|| (isEnforceListValue
			&& (resultEnforceValues === null || resultEnforceValues === undefined)))
	{
		throw "Auto-complete setup params error.";
	}

	ClearAutoComplete(fieldName);

	// must be above 0
	maxResults = (maxResults == null || maxResults <= 0) ? 10 : maxResults;

	// function to get the top 10 results partially matching the current value in the field from a pool of values
	var getResultSet = function(fieldNameParam, pool)
	{
		var userInput = GetControlValue(fieldNameParam);
		var resultSet = { results: [] };
		var userInputLowerCase = userInput.toLowerCase();

		if (isSort)
		{
			pool.sort(function(e1, e2)
			{
				return e1.name.localeCompare(e2.name);
			});
		}

		for (var i = 0; i < pool.length; i++)
		{
			var secondField = pool[i].name2;
			var thirdField = pool[i].name3;

			if (userInputLowerCase === pool[i].name.substring(0, userInputLowerCase.length).toLowerCase()
				|| (secondField && (userInputLowerCase === secondField.substring(0, userInputLowerCase.length).toLowerCase()))
				|| (thirdField && (userInputLowerCase === thirdField.substring(0, userInputLowerCase.length).toLowerCase())))
			{
				var fields = [pool[i].name];

				if (secondField)
				{
					fields.push(secondField);
				}

				if (thirdField)
				{
					fields.push(thirdField);
				}

				resultSet.results.push(
					{
						id: i,
						fields: fields
					});
			}

			if (resultSet.results.length >= maxResults)
			{
				break;
			}
		}

		return resultSet;
	};
	var keyPressHandler = function(ext)
	{
		try
		{
			var resultSet = getResultSet(fieldName, result);

			if (resultSet.results.length > 0)
			{
				ext.getEventSource().showAutoComplete(resultSet);
			}
			else
			{
				ext.getEventSource().hideAutoComplete();
			}
		}
		catch (e)
		{
			console.error(e);
			throw e.message;
		}
	};

	// keep the handler to be able to remove it later when removing auto-complete from field
	AutoCompleteKeyPressMap[fieldName] = keyPressHandler;
	AddOnKeyPress(fieldName, keyPressHandler);

	if (isEnforceListValue)
	{
		var onChangeHandler = function(ext)
		{
			var controlName = ext.getEventSource().getName();
			var controlValue = ext.getEventSource().getValue();

			if (controlValue == null || Contains(resultEnforceValues, null, controlValue))
			{
				ClearControlError(controlName);
			}
			else
			{
				ShowControlError(controlName, "Please choose a value from the auto-complete list.");
			}
		};

		// keep the handler to be able to remove it later when removing auto-complete from field
		AutoCompleteOnChangeMap[fieldName] = onChangeHandler;
		AddOnChange(fieldName, onChangeHandler);
	}

	if (callback)
	{
		callback(resultEnforceValues);
	}
}

function SetupEntityNameAutoComplete(entityNameFieldName, maxResults, callback)
{
	/// <summary>
	///     Adds entity names auto-complete to the field given.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>

	var result = [];
	var resultNames = [];

	ShowBusyIndicator('Loading entity names ... ', 736582);

	RetrieveEntityMetadata(null, ['LogicalName', 'DisplayName'], null, null, null,
		function(metadata)
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

			SetupAutoComplete(entityNameFieldName, result, resultNames, maxResults || 10, true, true, callback);
		},
		function(xhr, text)
		{
			HideBusyIndicator(736582);
			console.error(xhr);
			console.error(text);
		});
}

function SetupFieldNameAutoComplete(entityName, fieldNameFieldName, maxResults, callback, isUseCache)
{
	/// <summary>
	///     Adds entity field names auto-complete to the field given.
	/// If 'use cache' is true, then it will use previously loaded values.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	var entityMetadata = EntityMetadata[entityName];

	var loadCallback = function()
	{
		entityMetadata = EntityMetadata[entityName];

		if (!entityMetadata || entityMetadata.length <= 0)
		{
			console.error('SetupFieldNameAutoComplete: must load entity metadata first before setting auto-complete.');
			return;
		}

		var result = [];
		var resultNames = [];

		for (var i = 0; i < entityMetadata.length; i++)
		{
			var attributes = entityMetadata[i].Attributes;

			for (var j = 0; j < attributes.length; j++)
			{
				var fieldName = attributes[j].LogicalName;

				result.push(
					{
						id: attributes[j],
						name: fieldName,
						name2: (attributes[j].DisplayName && attributes[j].DisplayName.UserLocalizedLabel
									   && attributes[j].DisplayName.UserLocalizedLabel.Label)
								   ? attributes[j].DisplayName.UserLocalizedLabel.Label
								   : null
					});

				resultNames.push(fieldName);
			}
		}

		SetupAutoComplete(fieldNameFieldName, result, resultNames, maxResults || 10, true, true, callback);
	};

	if (entityMetadata && isUseCache)
	{
		loadCallback();
	}
	else
	{
		LoadEntityMetadata(entityName, loadCallback);
	}
}

function ClearAutoComplete(fieldName)
{
	/// <summary>
	///     Removes auto-complete from the given field.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	if (AutoCompleteKeyPressMap[fieldName])
	{
		RemoveOnKeyPress(fieldName, AutoCompleteKeyPressMap[fieldName]);
		delete AutoCompleteKeyPressMap[fieldName];
		ClearControlError(fieldName);
	}

	if (AutoCompleteOnChangeMap[fieldName])
	{
		RemoveOnChange(fieldName, AutoCompleteOnChangeMap[fieldName]);
		delete AutoCompleteOnChangeMap[fieldName];
		ClearControlError(fieldName);
	}
}

//#endregion

//#endregion

/////////////////////////////////////////////////////
//#region >>>>>>>>> Collection helpers <<<<<<<<<<< //

function Contains(array, keyOrEvaluator, value)
{
	/// <summary>
	///     Checks whether the array contains the value based on a key or property or evaluation function.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	/// <param name="array" type="object[]" optional="false"></param>
	/// <param name="keyOrEvaluator" type="string" optional="false">
	///     The key or property name, or the function to evaluate.
	///     <br />If 'null' is passed, then it will compare the whole object to the value.<br />The function should take an
	///     element from the array and return 'true' on match.
	/// </param>
	/// <param name="value" type="object" optional="false">The value to check for. Will be ignored if an evaluator is passed.</param>
	return Search(array, keyOrEvaluator, value, 1).length > 0;
}

function Remove(array, keyOrEvaluator, value, count)
{
	/// <summary>
	///     Removes all elements that pass the evaluator given.<br />
	///     Author: Ahmed Elsawalhy, credit: http://stackoverflow.com/a/5767357/1919456
	/// </summary>
	/// <param name="array" type="object[]" optional="false"></param>
	/// <param name="evaluator" type="function" optional="false">
	///     The function to use to test for match.<br />The function
	///     should take an element from the array and return 'true' on match.
	/// </param>
	/// <param name="value" type="object" optional="false">The value to check for. Will be ignored if an evaluator is passed.</param>
	/// <param name="count" type="number" optional="true">The max number of elements to remove. '-1' removes all.</param>
	var indexes;
	count = (count < 0 ? 0 : count) || count || array.length;

	while (count > 0)
	{
		indexes = Search(array, keyOrEvaluator, value, 1);

		if (!indexes.length)
		{
			break;
		}

		array.splice(indexes[0], 1);

		count--;
	}
}

function IsUnique(array, keyOrEvaluator, value)
{
	/// <summary>
	///     Checks whether the passed value for the given key or property or evaluation function is unique over all values in
	///     the array of objects.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	/// <param name="array" type="object[]" optional="false">The array of objects to search in.</param>
	/// <param name="keyOrEvaluator" type="string" optional="false">
	///     The key or property name, or the function to evaluate.
	///     <br />If 'null' is passed, then it will compare the whole object to the value.<br />The function should take an
	///     element from the array and return 'true' on match.
	/// </param>
	/// <param name="value" type="object" optional="false">The value to check for. Optional if a function was passed.</param>
	/// <returns type="bool">True or false.</returns>
	return Search(array, keyOrEvaluator, value, 2).length <= 1;
}

function Search(array, keyOrEvaluator, value, count, isReturnElements)
{
	/// <summary>
	///     Searches for all the objects that have a matching value for the given key or property or evaluation function.<br />
	///     Returns the indexes of the objects, or the objects that fit the criteria.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	/// <param name="array" type="object[]" optional="false">The array of objects to search in.</param>
	/// <param name="keyOrEvaluator" type="string" optional="false">
	///     The key or property name, or the function to evaluate.
	///     <br />If 'null' is passed, then it will compare the whole object to the value.<br />The function should take an
	///     element from the array and return 'true' on match.
	/// </param>
	/// <param name="value" type="object" optional="false">The value to check for. Will be ignored if an evaluator is passed.</param>
	/// <param name="count" type="number" optional="true">The max number of elements to return. '-1' returns all.</param>
	/// <param name="isReturnElements" type="bool" optional="true">
	///     If 'true', return the element themselves instead of the
	///     indexes.
	/// </param>
	/// <returns type="object[]">The array of indexes or objects for the elements matching.</returns>
	var result = [];
	count = count || -1;

	for (var i = 0; i < array.length && (count <= 0 || (count > 0 && result.length < count)); i++)
	{
		if (typeof (keyOrEvaluator) === "function"
				? keyOrEvaluator(array[i])
				: (keyOrEvaluator === null ? IsEqual(array[i], value) : IsEqual(array[i][keyOrEvaluator], value)))
		{
			result.push(isReturnElements ? array[i] : i);
		}
	}

	return result;
}

function Count(array, keyOrEvaluator, value)
{
	/// <summary>
	///     Count all objects that have a matching value for the given key or property or evaluation function.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	/// <param name="array" type="object[]" optional="false">The array of objects to search in.</param>
	/// <param name="keyOrEvaluator" type="string" optional="false">
	///     The key or property name, or the function to evaluate.
	///     <br /> If 'null' is passed, then it will compare the whole object to the value.<br />The function should take an
	///     element from the array and return 'true' on match.
	/// </param>
	/// <param name="value" type="object" optional="false">The value to check for.</param>
	/// <returns type="number">The count of objects matching.</returns>
	return Search(array, keyOrEvaluator, value).length;
}

function Copy(obj, isDeep)
{
	/// <summary>
	///     Copies the object properties into a new object, and all references within if needed.<br />
	///     Author: Ahmed Elsawalhy, credit: http://stackoverflow.com/a/23536726/1919456
	/// </summary>
	/// <param name="obj" type="object" optional="false">The object to copy.</param>
	/// <param name="isDeep" type="bool" optional="true">If 'true', recursively copies objects within as well.</param>
	/// <returns type="object">The new object.</returns>
	var v, key;
	var output = Array.isArray(obj) ? [] : {};

	for (key in obj)
	{
		if (obj.hasOwnProperty(key))
		{
			v = obj[key];
			output[key] = (typeof v === "object" && isDeep) ? copy(v) : v;
		}
	}

	return output;
}

function IndexOfNonStrict(array, value)
{
	/// <summary>
	///     Compares using 'IsEqual' and returns the index of the value in the array.
	/// </summary>
	/// <param name="array" type="object[]" optional="false">
	///     Array to iterate over.
	/// </param>
	/// <param name="value" type="object" optional="false">
	///     Value to compare.
	/// </param>
	var index = -1;

	for (var i = 0; i < array.length; i++)
	{
		if (IsEqual(array[i], value))
		{
			return index;
		}
	}

	return index;
}

//#endregion

///////////////////////////////////////////////
//#region >>>>>>>>> Math helpers <<<<<<<<<<< //

function Sum(array)
{
	/// <summary>
	/// Sums the values in the array and returns the result.
	///     Author: Ahmed Elsawalhy<br />
	///     credit: http://stackoverflow.com/a/1669222/1919456
	/// </summary>
	return array.reduce(function(a, b)
	{
		return a + b;
	}, 0);
}

function Min(array)
{
	/// <summary>
	/// Returns the minimum value in the array.
	///     Author: Ahmed Elsawalhy<br />
	///     credit: http://stackoverflow.com/a/1669222/1919456
	/// </summary>
	return Math.min.apply(null, array);
}

function Max(array)
{
	/// <summary>
	/// Returns the maximum value in the array.
	///     Author: Ahmed Elsawalhy<br />
	///     credit: http://stackoverflow.com/a/1669222/1919456
	/// </summary>
	return Math.max.apply(null, array);
}

function Average(array)
{
	/// <summary>
	/// Returns the average of the array.
	///     Author: Ahmed Elsawalhy<br />
	///     credit: http://www.jstips.co/en/array-average-and-median/
	/// </summary>
	var temp = array.slice();
	var sum = temp.reduce(function(previous, current)
	{
		return current + previous;
	});
	return sum / temp.length;
}

function Median(array)
{
	/// <summary>
	/// Returns the median of the array.
	///     Author: Ahmed Elsawalhy<br />
	///     credit: http://www.jstips.co/en/array-average-and-median/
	/// </summary>
	var temp = array.slice();
	temp.sort(function(a, b)
	{
		return a - b;
	});
	return (temp[(temp.length - 1) >> 1] + temp[temp.length >> 1]) / 2;
}

function AverageFields(inputFields)
{
	/// <summary>
	/// Averages the values in the fields and returns the result. Skips 'null' values.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	return Average(GetFieldsValues(inputFields, true));
}

function SumFields(inputFields)
{
	/// <summary>
	/// Sums the values in the fields and returns the result. Skips 'null' values.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	return Sum(GetFieldsValues(inputFields, true));
}

function MultiplyFields(inputFields)
{
	/// <summary>
	/// Multiplies the values in the fields and returns the result. Skips 'null' values.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	return GetFieldsValues(inputFields, true).reduce(function (a, b)
	{
		return a * b;
	}, 1);
}

//#endregion

//////////////////////////////////////////////
//#region >>>>>>>>> CRM helpers <<<<<<<<<<< //

function ExecuteOnNotDirty(callback)
{
	/// <summary>
	///     Keeps checking the form until it is saved, and then execute the function given.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>

	// keep checking
	setTimeout(
		function()
		{
			if (IsFormDirty())
			{
				ExecuteOnNotDirty(callback);
			}
			else
			{
				if (callback)
				{
					callback();
				}
			}
		}, 100);
}

function RefreshOnNotDirty()
{
	/// <summary>
	///     Keeps checking the form until it is saved, and then it reloads the form.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>

	// keep checking
	setTimeout(
		function()
		{
			// make sure that everything is saved before refresh
			if (IsFormDirty())
			{
				RefreshOnNotDirty();
			}
			else
			{
				Xrm.Utility.openEntityForm(GetEntityName(), GetRecordId());
			}
		}, 100);
}

function GoToRecord(entityName, recordId)
{
	/// <summary>
	///     Author: Ahmed Elsawalhy
	/// </summary>

	Xrm.Utility.openEntityForm(entityName, recordId);
}

function GetOrgUrl()
{
	return Xrm.Page.context.getClientUrl();
}

var CrmVersion = {
		_2011: '2011',
		_2013: '2013',
		_2015: '2015',
		_2016: '2016'
	};

function GetCrmVersion()
{
	/// <summary>
	///     Returns the current CRM version in years. Use 'CrmVersion' for easy comparison.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	if (GetControl(GetFormFieldNames[0])['addOnKeyPress'])
	{
		return CrmVersion._2016;
	}

	if (Xrm.Page.context['isOutlookClient'])
	{
		return CrmVersion._2015;
	}

	if (Xrm.Page.ui['setFormNotification'])
	{
		return CrmVersion._2013;
	}

	return CrmVersion._2011;
}

function GetRecordId(isClean)
{
	/// <summary>
	///     Returns the current record ID. Cleaning removes the braces around the ID if required.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	var id = Xrm.Page.data.entity.getId();
	return isClean ? id.replace('{', '').replace('}', '') : id;
}

function GetUserId(isClean)
{
	/// <summary>
	///     Returns the current user's ID. Cleaning removes the braces around the ID if required.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	var id = Xrm.Page.context.getUserId();
	return isClean ? id.replace('{', '').replace('}', '') : id;
}

var Language = {
		ENGLISH: 1033,
		ARABIC: 1025
	};

function GetUserLanguageCode()
{
	/// <summary>
	///     Returns the language code of the current user's interface. Use 'Language' for easy comparison.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	return Xrm.Page.context.getUserLcid();
}

function UserHasRole(roleName)
{
	var url = Xrm.Page.context.getClientUrl();
	var oDataEndpointUrl = url + "/XRMServices/2011/OrganizationData.svc/";
	oDataEndpointUrl += "RoleSet?$filter=Name eq '" + roleName + "'";

	var service = GetRequestObject();

	if (service != null)
	{
		service.open("GET", oDataEndpointUrl, false);
		service.setRequestHeader("X-Requested-Width", "XMLHttpRequest");
		service.setRequestHeader("Accept", "application/json, text/javascript, */*");
		service.send(null);

		var requestResults = eval('(' + service.responseText + ')').d;

		if (requestResults != null)
		{
			for (var j = 0; j < requestResults.results.length; j++)
			{
				var role = requestResults.results[j];

				var id = role.RoleId;

				var currentUserRoles = Xrm.Page.context.getUserRoles();

				for (var i = 0; i < currentUserRoles.length; i++)
				{
					var userRole = currentUserRoles[i];

					if (IsGuidEqual(userRole, id))
					{
						return true;
					}
				}
			}
		}
	}

	return false;
}

function UserHasRoleId(roleId)
{
	/// <summary>
	///     Author: Ahmed Elsawalhy
	/// </summary>
	var currentUserRoles = Xrm.Page.context.getUserRoles();

	for (var i = 0; i < currentUserRoles.length; i++)
	{
		var userRole = currentUserRoles[i];

		if (IsGuidEqual(userRole, roleId))
		{
			return true;
		}
	}

	return false;
}


function CanUserEditRecord(ownerId, entitySetName, recordId, callback, errorCallback) {
	/// <summary>
	///     Author: Ahmed Elsawalhy
	/// </summary>
	$.ajax({
		type: "GET",
		contentType: "application/json; charset=utf-8",
		datatype: "json",
		url: Xrm.Page.context.getClientUrl() + "/api/data/v8.2/systemusers(" + ownerId + ")/Microsoft.Dynamics.CRM.RetrievePrincipalAccess(Target=@tid)?"
			+ "@tid={'@odata.id':'" + entitySetName + "(" + recordId + ")'}",
		beforeSend: function (xmlHttpRequest) {
			xmlHttpRequest.setRequestHeader("OData-MaxVersion", "4.0");
			xmlHttpRequest.setRequestHeader("OData-Version", "4.0");
			xmlHttpRequest.setRequestHeader("Accept", "application/json");
		},
		async: true,
		success: function (data, textStatus, xhr) {
			if (data && data.AccessRights && callback) {
				callback(data.AccessRights);
			}
		},
		error: function (xhr, textStatus, errorThrown) {
			console.error(xhr);
			console.error(textStatus);
			console.error(errorThrown);

			if (errorCallback) {
				errorCallback(xhr);
			}
		}
	});
}

function LoadWebResources(resources, callback, scopeWindow)
{
	/// <summary>
	///     Takes an array of resource names and loads them into the current context using "LoadScript".<br />
	///     The resources param accepts a string as well in case a single resource is needed instead.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	/// <param name="resources" type="String[] | string" optional="false">The resource[s] to load.</param>
	/// <param name="callback" type="Function" optional="true">A function to call after resource[s] has been loaded.</param>
	if (resources.length <= 0)
	{
		if (callback)
		{
			callback();
		}

		return;
	}

	if (typeof resources === 'string')
	{
		resources = [resources];
	}

	var localCallback = function()
	{
		if (resources.length > 1)
		{
			LoadWebResources(resources.slice(1, resources.length), callback, scopeWindow);
		}
		else
		{
			if (callback)
			{
				callback();
			}
		}
	};

	LoadScript(Xrm.Page.context.getClientUrl() + '/WebResources/' + resources[0], localCallback, scopeWindow);
}

function LoadWebResourceCss(fileName, scopeWindow)
{
	// modified it to be generic -- Sawalhy
	LoadCss(Xrm.Page.context.getClientUrl() + '/WebResources/' + fileName, scopeWindow);
}

function IsValueUnique(entitySetName, primaryKey, fieldName, value, callback, errorCallback)
{
	/// <summary>
	///     Checks using WebAPI whether the field value is unique among the entity records.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	/// <param name="entitySetName" type="string" optional="false">The entity 'set' name as used by WebAPI.</param>
	/// <param name="primaryKey" type="string" optional="false">The primary key logical name of the entity.</param>
	/// <param name="fieldName" type="string" optional="false">The field name to check the value of.</param>
	/// <param name="value" type="string" optional="false">The value to check for. Wrap it in single-quotes if it is a string</param>
	/// <param name="callback" type="type" optional="true">Callback to call after finishing.</param>
	/// <param name="errorCallback" type="type" optional="true">Error callback.</param>
	IsValuesUnique(entitySetName, primaryKey, [{ key: fieldName, value: value }], callback, errorCallback);
}

function IsValuesUnique(entitySetName, primaryKey, keyValArray, callback, errorCallback, isSuppressDefault)
{
	/// <summary>
	///     Checks using WebAPI whether the field values are unique among the entity records. It compares using 'AND'.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	/// <param name="entitySetName" type="string" optional="false">The entity 'set' name as used by WebAPI.</param>
	/// <param name="primaryKey" type="string" optional="false">The primary key logical name of the entity.</param>
	/// <param name="keyValArray" type="string[]" optional="false">The list of keys and values to compare using 'AND'.
	/// In the form: [{ key: key1, value: value1 }].</param>
	/// <param name="callback" type="type" optional="true">Callback to call after finishing.</param>
	/// <param name="errorCallback" type="type" optional="true">Error callback.</param>
	/// <param name="isSuppressDefault" type="bool" optional="true">If 'true', suppress the default error message.</param>

	var guidRegex = /^[{(]?[0-9A-F]{8}[-]?([0-9A-F]{4}[-]?){3}[0-9A-F]{12}[)}]?$/gi;

	$.ajax({
			type: "GET",
			contentType: "application/json; charset=utf-8",
			datatype: "json",
			url: GetOrgUrl() + "/api/data/v8.1/" + entitySetName + "?" +
				"$select=" + primaryKey +
				"&$filter=" + keyValArray.reduce(function(accumulated, element)
				{
					return accumulated + (accumulated ? ' and ' : '') +
						(element.value.toString().match(guidRegex) ? ('_' + element.key + '_value') : element.key) +
						' eq ' + element.value;
				}, '') +
				(GetFormType() === FormType.Create
					 ? ''
					 : " and  " + primaryKey + " ne " + GetRecordId(true)) +
				"&$count=true",
			beforeSend: function(xmlHttpRequest)
			{
				xmlHttpRequest.setRequestHeader("OData-MaxVersion", "4.0");
				xmlHttpRequest.setRequestHeader("OData-Version", "4.0");
				xmlHttpRequest.setRequestHeader("Accept", "application/json");
			},
			async: true,
			success: function(data, textStatus, xhr)
			{
				var count = data['@odata.count'];

				if (count > 0)
				{
					if (!isSuppressDefault)
					{
						for (var i = 0; i < keyValArray.length; i++)
						{
							ShowControlError(keyValArray[i].key, '"' + keyValArray[i].key + '" value is used by another record.' +
								' Please enter a unique value.');
						}
					}
				}
				else
				{
					if (!isSuppressDefault)
					{
						for (var i = 0; i < keyValArray.length; i++)
						{
							ClearControlError(keyValArray[i].key);
						}
					}
				}

				if (callback)
				{
					callback(count <= 0);
				}
			},
			error: function(xhr, textStatus, errorThrown)
			{
				var text = textStatus + " " + errorThrown;

				if (errorCallback)
				{
					errorCallback(xhr, text);
				}
				else
				{
					console.error(xhr);
					console.error(text);
				}
			}
		});
}

function RetrieveEntityById(entityWebApiName, id, fields, callback, errorCallback)
{
	/// <summary>
	///     Retrieves the entity with the given ID. The result is passed to the callback as an argument.
	///     The result's fields can be accessed by their logical name directly (result.field).<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	/// <param name="entityWebApiName" type="string">The name of the entity in the WebAPI service (special plural).</param>
	/// <param name="fields" type="string[]">An array of field names to retrieve. If 'null', all will be retrieved.</param>
	/// <param name="errorCallback" type="function">Error message will be passed as parameter.</param>
	var clientUrl = Xrm.Page.context.getClientUrl();
	var oDataPath = clientUrl + "/api/data/v8.1";

	$.ajax({
			type: "GET",
			contentType: "application/json; charset=utf-8",
			datatype: "json",
			url: oDataPath + "/" + entityWebApiName + "(" + id + ")" + (fields ? "?$select=" + fields : ""),
			beforeSend: function(xmlHttpRequest)
			{
				xmlHttpRequest.setRequestHeader("OData-MaxVersion", "4.0");
				xmlHttpRequest.setRequestHeader("OData-Version", "4.0");
				xmlHttpRequest.setRequestHeader("Accept", "application/json");
			},
			async: true,
			success: function(data, textStatus, xhr)
			{
				if (callback)
				{
					callback(data);
				}
			},
			error: function(xhr, textStatus, errorThrown)
			{
				console.error(xhr);
				console.error(textStatus + ": " + errorThrown);

				if (errorCallback)
				{
					errorCallback(textStatus + ": " + errorThrown);
				}
			}
		});
}

function RetrieveRelatedEntities(entityWebApiName, relationName, relatedFields, callback, errorCallback)
{
	/// <summary>
	///     Retrieves the related records of the current entity. The result is passed to the callback as an argument.
	///     The result's can be accessed through the array and then fields by their logical name directly ([result.field]).
	///     <br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	/// <param name="entityWebApiName" type="string">The name of the entity in the WebAPI service (special plural).</param>
	/// <param name="relationName" type="string">Schema name of the relationship.</param>
	/// <param name="relatedFields" type="string[]">An array of field names to retrieve. If 'null', all will be retrieved.</param>
	/// <param name="errorCallback" type="function">Error message will be passed as parameter.</param>
	var clientUrl = Xrm.Page.context.getClientUrl();
	var oDataPath = clientUrl + "/api/data/v8.1";

	$.ajax({
			type: "GET",
			contentType: "application/json; charset=utf-8",
			datatype: "json",
			url: oDataPath + "/" + entityWebApiName + "(" + GetRecordId(true) + ")?$select=createdon" +
				"&$expand=" + relationName + (relatedFields ? "($select=" + relatedFields + ")" : ""),
			beforeSend: function(xmlHttpRequest)
			{
				xmlHttpRequest.setRequestHeader("OData-MaxVersion", "4.0");
				xmlHttpRequest.setRequestHeader("OData-Version", "4.0");
				xmlHttpRequest.setRequestHeader("Accept", "application/json");
			},
			async: true,
			success: function(data, textStatus, xhr)
			{
				if (callback)
				{
					callback(data[relationName]);
				}
			},
			error: function(xhr, textStatus, errorThrown)
			{
				console.error(xhr);
				console.error(textStatus + ": " + errorThrown);

				if (errorCallback)
				{
					errorCallback(textStatus + ": " + errorThrown);
				}
			}
		});
}

function ODataRequestJSONParsed(oDataRequestString)
{
	var retrieveEventListsReq = new XMLHttpRequest();
	var serverUrl = Xrm.Page.context.getClientUrl();
	var oDataPath = serverUrl + "/XRMServices/2011/OrganizationData.svc";

	oDataPath += oDataRequestString;
	retrieveEventListsReq.open("GET", oDataPath, false);
	retrieveEventListsReq.setRequestHeader("Accept", "application/json");
	retrieveEventListsReq.setRequestHeader("Content-Type", "application/json; charset=utf-8");
	retrieveEventListsReq.send();

	if (retrieveEventListsReq.status === 200)
	{
		return JSON.parse(retrieveEventListsReq.responseText).d;
	}
	else
	{
		return null;
	}
}

//#region >>>>>>>>> Retrieve OData 2 <<<<<<<<<<< //

//retrieve odata object using ... Attribute Name, the ID of the record , Schema Name of the entity
function RetrieveOdataObject(id, schemaName)
{
	return ODataRequestJSONParsed("/" + schemaName + "Set(guid'" + id + "')");
}

function RetrieveOdataObjectFiltered(id, schemaName, selectedField, filterField)
{
	return ODataRequestJSONParsed("/" + schemaName + "Set?$select=" + selectedField + "&$filter=" + filterField + " eq guid'" + id + "'");
}

function RetrieveOdataObjectFilteredOptionSet(schemaName, filterField, optionValue)
{
	return ODataRequestJSONParsed("/" + schemaName + "Set?$filter=" + filterField + "/Value eq " + optionValue + "");
}

function RetrieveOdataObjectFilteredLookup(schemaName, filterField, lookupid)
{
	return ODataRequestJSONParsed("/" + schemaName + "Set?$filter=" + filterField + "/Id eq guid'" + lookupid + "'");
}

//#endregion

function OpenReportByParamaters(reportName, reportParameters)
{
	var result = ODataRequestJSONParsed("/ReportSet?$select=*&$filter=substringof('" + reportName + "',Description)");

	if (result.results.length > 0)
	{
		var serverUrl = Xrm.Page.context.getClientUrl();
		var rdlName = result.results[0].Name + ".rdl";
		var reportGuid = result.results[0].ReportId.replace('{', '').replace('}', '');
		var entityGuid = Xrm.Page.data.entity.getId();//Here I am getting Entity GUID it from it's form
		var entityType = Xrm.Page.context.getQueryStringParameters().etc;
		var link = serverUrl + "/crmreports/viewer/viewer.aspx?action=run&context=records&helpID=" + rdlName + "&id={" + reportGuid + "}&records=" + entityGuid + "&recordstype=" + entityType + reportParameters;
		var randomnumber = Math.floor((Math.random() * 10000) + 1);
		window.open(link, "reportwindow" + randomnumber, "resizable=1,width=950,height=700");
	}
}

//#region >>>>>>>>> Metadata <<<<<<<<<<< //

function GetEntityName()
{
	return Xrm.Page.data.entity.getEntityName();
}

function GetEntityWebApiName(logicalName, callback, errorCallback)
{
	/// <summary>
	/// Retrieves the entity set name used in CRM WebAPI service. The callback takes the name returned as argument.<br />
	/// Author: Ahmed Elsawalhy
	/// </summary>
	RetrieveEntityMetadata(logicalName, 'LogicalCollectionName', null, null, null, function(metadata)
	{
		if (callback && metadata)
		{
			callback(metadata[0].LogicalCollectionName);
		}
	}, errorCallback);
}

function GetObjectTypeCode(entityName)
{
	try
	{
		if (typeof window.RemoteCommand === "undefined")
		{
			window.RemoteCommand = parent.RemoteCommand;
		}

		var lookupService = new window.RemoteCommand("LookupService", "RetrieveTypeCode");
		lookupService.SetParameter("entityName", entityName);
		var result = lookupService.Execute();

		if (result.Success && typeof result.ReturnValue == "number")
		{
			return result.ReturnValue;
		}
		else
		{
			return null;
		}
	}
	catch (ex)
	{
		throw ex;
	}
}

function RetrieveEntityMetadata(logicalName, properties, condition, attributeProperties, attributesCondition,
		callback, errorCallback)
	{
		/// <summary>
		///     Loads entity's metadata. Currently limited to properties and attributes only.<br />
		///     To load all entities, pass null to 'logicalName'.<br />
		///     Author: Ahmed Elsawalhy
		/// </summary>
		/// <param name="logicalName" type="string" optional="true">
		///     The name of the entity to retrieve. Pass 'null' to retrieve all entities.
		/// </param>
		/// <param name="properties" type="object" optional="true">
		///     The CSV string or array of the property names to retrieve. Pass '*' to load all properties, or null to load none.
		/// </param>
		/// <param name="condition" type="string" optional="true">The condition on the properties in OData format.</param>
		/// <param name="attributeProperties" type="object" optional="true">
		///     The CSV string or array of the property names to retrieve.
		///     Must pass the list of properties to load if attributes are needed.
		/// </param>
		/// <param name="attributesCondition" type="string" optional="true">The condition on the properties in OData format</param>
		/// <param name="callback" type="function" optional="false">
		///     The callback function. The metadata object is passed., which contains a list of entities and their 'Attributes'.
		/// </param>
		/// <param name="errorCallback" type="function" optional="true">Error callback. The 'XHR' and error text are passed.</param>
		if (!callback)
		{
			console.error('Callback function is missing.');
			throw 'Callback function is missing.';
		}

		properties = (properties && properties.indexOf('*') >= 0) ? null : (properties || 'LogicalName');
		condition = condition || (logicalName ? "LogicalName eq '" + logicalName + "'" : '');

		attributesCondition = attributesCondition || "AttributeOf eq null";

		$.ajax({
				type: "GET",
				contentType: "application/json; charset=utf-8",
				datatype: "json",
				url: Xrm.Page.context.getClientUrl() + "/api/data/v8.1/EntityDefinitions?" +
					(properties ? ("$select=" + properties) : '') +
					(condition ? ((properties ? '&' : '') + "$filter=" + condition) : '') +
					(attributeProperties
						 ? ((properties || condition ? '&' : '') +
							 "$expand=Attributes(" +
							 "$select=" + attributeProperties +
							 ";$filter=" + attributesCondition + ")")
						 : ''),
				beforeSend: function(xmlHttpRequest)
				{
					xmlHttpRequest.setRequestHeader("OData-MaxVersion", "4.0");
					xmlHttpRequest.setRequestHeader("OData-Version", "4.0");
					xmlHttpRequest.setRequestHeader("Accept", "application/json");
				},
				async: true,
				success: function(data, textStatus, xhr)
				{
					if (callback)
					{
						callback(data.value);
					}
				},
				error: function(xhr, textStatus, errorThrown)
				{
					var text = textStatus + ": " + errorThrown;

					if (errorCallback)
					{
						errorCallback(xhr, text);
					}
					else
					{
						console.error(xhr);
						console.error(text);
					}
				}
			});
	}

function RetrieveEntityFields(entityName, callback, errorCallback)
{
	/// <summary>
	///     Returns an array of fields in the form of '[{text, value}]'.
	///     Author: Ahmed Elsawalhy
	/// </summary>
	/// <param name="logicalName" type="string" optional="true">
	///     The name of the entity to retrieve fields for.
	/// </param>
	var entityMetadata = EntityMetadata[entityName];

	var loadCallback = function()
	{
		entityMetadata = EntityMetadata[entityName];

		if (!entityMetadata || entityMetadata.length <= 0)
		{
			var message = 'RetrieveEntityFields: must load entity metadata first.';

			console.error(message);

			if (errorCallback)
			{
				errorCallback(message);
			}

			return;
		}


		var resultMap = [];

		for (var i = 0; i < entityMetadata.length; i++)
		{
			var attributes = entityMetadata[i].Attributes;

			for (var j = 0; j < attributes.length; j++)
			{
				var fieldName = attributes[j].LogicalName;

				resultMap.push({
					text: (attributes[j].DisplayName && attributes[j].DisplayName.UserLocalizedLabel
								  && attributes[j].DisplayName.UserLocalizedLabel.Label)
							  ? attributes[j].DisplayName.UserLocalizedLabel.Label
							  : null,
					value: fieldName
				});
			}
		};

		if (callback)
		{
			callback(resultMap);
		}
	}

	if (entityMetadata)
	{
		loadCallback();
	}
	else
	{
		LoadEntityMetadata(entityName, loadCallback, errorCallback);
	}
}

var EntityMetadata = window.EntityMetadata || {};

function LoadEntityMetadata(entityName, callback, errorCallback)
{
    /// <summary>
	/// Internal use only!
    /// </summary>
	ShowBusyIndicator('Loading entity metadata ... ', 9156774);

	RetrieveEntityMetadata(entityName, null, null, ['LogicalName', 'DisplayName'], null,
		function(metadata)
		{
			HideBusyIndicator(9156774);
			EntityMetadata[entityName] = metadata;

			if (callback)
			{
				callback();
			}
		},
		function(xhr, text)
		{
			HideBusyIndicator(9156774);
			console.error(xhr);
			console.error(text);

			if (errorCallback)
			{
				errorCallback();
			}
		});
}

function RetrieveOptionSetValueLabel(entityName, fieldName, value, languageCode, callback, errorCallback)
{
	/// <summary>
	///     Retrieves the label of the option-set value corresponding to the language passed.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	if (!callback)
	{
		console.error('Callback function is missing.');
		throw 'Callback function is missing.';
	}

	RetrieveEntityMetadata(entityName, 'MetadataId', null, 'MetadataId', "LogicalName eq '" + fieldName + "'",
		function(metadata)
		{
			$.ajax({
					type: "GET",
					contentType: "application/json; charset=utf-8",
					datatype: "json",
					url: Xrm.Page.context.getClientUrl() + "/api/data/v8.1/EntityDefinitions" +
						"(" + metadata[0].MetadataId + ")" +
						"/Attributes" +
						"(" + metadata[0].Attributes[0].MetadataId + ")" +
						"/Microsoft.Dynamics.CRM.PicklistAttributeMetadata" +
						"?$select=LogicalName&$expand=OptionSet,GlobalOptionSet",
					beforeSend: function(xmlHttpRequest)
					{
						xmlHttpRequest.setRequestHeader("OData-MaxVersion", "4.0");
						xmlHttpRequest.setRequestHeader("OData-Version", "4.0");
						xmlHttpRequest.setRequestHeader("Accept", "application/json");
					},
					async: true,
					success: function(data, textStatus, xhr)
					{
						var optionSet = data.GlobalOptionSet || data.OptionSet;

						if (optionSet)
						{
							var options = optionSet.Options;

							if (options)
							{
								var option = Search(options, 'Value', value, 1, true)[0];

								if (option)
								{
									var labels = option.Label.LocalizedLabels;

									if (labels)
									{
										var label = Search(labels, 'LanguageCode', languageCode || 1033, 1, true)[0];

										if (label && callback)
										{
											callback(label.Label);
											return;
										}
									}
								}
							}
						}

						if (callback)
						{
							callback(null);
						}
					},
					error: function(xhr, textStatus, errorThrown)
					{
						var text = textStatus + " " + errorThrown;

						if (errorCallback)
						{
							errorCallback(xhr, text);
						}
						else
						{
							console.error(xhr);
							console.error(text);
						}
					}
				});
		});
}

function RetrieveOptionSetValuesLabels(entityName, fieldName, callback, errorCallback)
{
	/// <summary>
	///     Retrieves the values and labels of the option-set corresponding to all languages in the system.<br />
	///     The object returned is an array in the format: [{value: value, labels: [{code: code, label: label]}]<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	if (!callback)
	{
		console.error('Callback function is missing.');
		throw 'Callback function is missing.';
	}

	RetrieveEntityMetadata(entityName, 'MetadataId', null, 'MetadataId', "LogicalName eq '" + fieldName + "'",
		function(metadata)
		{
			$.ajax({
					type: "GET",
					contentType: "application/json; charset=utf-8",
					datatype: "json",
					url: Xrm.Page.context.getClientUrl() + "/api/data/v8.1/EntityDefinitions" +
						"(" + metadata[0].MetadataId + ")" +
						"/Attributes" +
						"(" + metadata[0].Attributes[0].MetadataId + ")" +
						"/Microsoft.Dynamics.CRM.PicklistAttributeMetadata" +
						"?$select=LogicalName&$expand=OptionSet,GlobalOptionSet",
					beforeSend: function(xmlHttpRequest)
					{
						xmlHttpRequest.setRequestHeader("OData-MaxVersion", "4.0");
						xmlHttpRequest.setRequestHeader("OData-Version", "4.0");
						xmlHttpRequest.setRequestHeader("Accept", "application/json");
					},
					async: true,
					success: function(data, textStatus, xhr)
					{
						var optionSet = data.GlobalOptionSet || data.OptionSet;
						var resultMap = [];

						if (optionSet)
						{
							var options = optionSet.Options;

							if (options)
							{
								for (var i = 0; i < options.length; i++)
								{
									var value = options[i].Value;
									var labels = [];
									var rawLabels = options[i].Label.LocalizedLabels;

									if (rawLabels)
									{
										for (var j = 0; j < rawLabels.length; j++)
										{
											labels.push({ code: rawLabels[j].LanguageCode, label: rawLabels[j].Label });
										}
									}

									resultMap.push({ value: value, labels: labels });
								}

								if (callback)
								{
									callback(resultMap);
								}
							}

							if (callback)
							{
								callback(null);
							}
						}
					},
					error: function(xhr, textStatus, errorThrown)
					{
						var text = textStatus + " " + errorThrown;

						if (errorCallback)
						{
							errorCallback(xhr, text);
						}
						else
						{
							console.error(xhr);
							console.error(text);
						}
					}
				});
		});
}

function RetrieveGlobalOptionSetValueLabel(optionSetName, value, languageCode, callback, errorCallback)
{
	/// <summary>
	///     Retrieves the label of the option-set value corresponding to the language passed.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	if (!callback)
	{
		console.error('Callback function is missing.');
		throw 'Callback function is missing.';
	}

	var req = new XMLHttpRequest();
	req.open("GET", GetOrgUrl() + "/api/data/v8.1/GlobalOptionSetDefinitions?$select=Name", true);

	req.setRequestHeader("Accept", "application/json");
	req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
	req.setRequestHeader("OData-MaxVersion", "4.0");
	req.setRequestHeader("OData-Version", "4.0");

	req.onreadystatechange = function()
	{
		if (this.readyState === 4)
		{
			req.onreadystatechange = null;

			if (this.status === 200)
			{
				var data = JSON.parse(this.response);

				if (data)
				{
					data = data.value;

					if (data)
					{
						var optionSet = Search(data, 'Name', optionSetName, 1, true)[0];

						if (optionSet && optionSet.MetadataId)
						{
							var req2 = new XMLHttpRequest();
							req2.open("GET", GetOrgUrl() + "/api/data/v8.1/GlobalOptionSetDefinitions(" + optionSet.MetadataId + ")", true);

							req2.setRequestHeader("Accept", "application/json");
							req2.setRequestHeader("Content-Type", "application/json; charset=utf-8");
							req2.setRequestHeader("OData-MaxVersion", "4.0");
							req2.setRequestHeader("OData-Version", "4.0");

							req2.onreadystatechange = function()
							{
								if (this.readyState === 4)
								{
									req2.onreadystatechange = null;

									if (this.status === 200)
									{
										var data2 = JSON.parse(this.response);

										if (data2)
										{
											var options = data2.Options;

											if (options)
											{
												var option = Search(options, 'Value', value, 1, true)[0];

												if (option)
												{
													var labels = option.Label.LocalizedLabels;

													if (labels)
													{
														var label = Search(labels, 'LanguageCode', languageCode || 1033, 1, true)[0];

														if (label && callback)
														{
															callback(label.Label);
															return;
														}
													}
												}
											}
										}

										if (callback)
										{
											callback(null);
										}
									}
									else
									{
										var error = JSON.parse(this.response).error;
										console.error(error);

										if (errorCallback)
										{
											errorCallback(error.message);
										}
									}
								}
							};

							req2.send();
							return;
						}
					}
				}

				if (callback)
				{
					callback(null);
				}
			}
			else
			{
				var error = JSON.parse(this.response).error;
				console.error(error);

				if (errorCallback)
				{
					errorCallback(error.message);
				}
			}
		}
	};

	req.send();
}

//#endregion

//#endregion

///////////////////////////////////////////////
//#region >>>>>>>>> Misc helpers <<<<<<<<<<< //

function LoadCss(path, scopeWindow)
{
	scopeWindow = scopeWindow || window;
	var head = scopeWindow.document.getElementsByTagName('head')[0];
	var link = scopeWindow.document.createElement('link');
	link.rel = 'stylesheet';
	link.type = 'text/css';
	link.href = path;
	link.media = 'all';
	head.appendChild(link);
}

function LoadScript(url, callback, scopeWindow)
{
	/// <summary>
	///     Takes a URL of a script file and loads it into the current context, and then calls the function passed.<br />
	///     Author: Ahmed Elsawalhy<br />
	///     credit: http://stackoverflow.com/a/950146/1919456
	/// </summary>
	/// <param name="url" type="String" optional="false">The URL to the script file.</param>
	/// <param name="callback" type="Function" optional="true">The function to call after loading the script.</param>

	scopeWindow = scopeWindow || window;
	// Adding the script tag to the head as suggested before
	var head = scopeWindow.document.getElementsByTagName('head')[0];
	var script = scopeWindow.document.createElement('script');
	script.type = 'text/javascript';
	script.src = url;

	// Then bind the event to the callback function.
	// There are several events for cross browser compatibility.

	if (callback)
	{
		//script.onreadystatechange = callback;
		script.onload = callback;
	}

	// Fire the loading
	head.appendChild(script);
}

function WaitForObjects(objects, callback)
{
	/// <summary>
	///     Takes an array of object names to wait for until they are available in memory, and then calls a function.<br />
	///     The objects param accepts a string as well in case a single object is needed instead.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	/// <param name="resources" type="String[] | string" optional="false">The object[s] to wait for.</param>
	/// <param name="callback" type="Function" optional="false">A function to call after the object[s] is available.</param>
	if (objects.length <= 0)
	{
		if (callback)
		{
			callback();
		}

		return;
	}

	if (typeof objects === 'string')
	{
		objects = [objects];
	}

	if (window[objects[0]] === undefined)
	{
		setTimeout(function()
		{
			WaitForObjects(objects, callback);
		}, 500);

		return;
	}
	else if (objects.length > 1)
	{
		WaitForObjects(objects.slice(1, objects.length), callback);
		return;
	}

	if (callback)
	{
		callback();
	}
}

function IsGuidEqual(guid1, guid2)
{
	var isEqual = !guid1 && !guid2;

	if (guid1 && guid2)
	{
		isEqual = guid1.replace(/[{}]/g, "").toLowerCase() === guid2.replace(/[{}]/g, "").toLowerCase();
	}

	return isEqual;
}

function IsEqual(x, y)
{
	'use strict';

	if (x === null || x === undefined || y === null || y === undefined)
	{
		return x === y;
	}
	// after this just checking type of one would be enough
	if (x.constructor !== y.constructor)
	{
		return false;
	}
	// if they are functions, they should exactly refer to same one (because of closures)
	if (x instanceof Function)
	{
		return x === y;
	}
	// if they are regexps, they should exactly refer to same one (it is hard to better equality check on current ES)
	if (x instanceof RegExp)
	{
		return x === y;
	}
	if (x === y || x.valueOf() === y.valueOf())
	{
		return true;
	}
	if (Array.isArray(x) && x.length !== y.length)
	{
		return false;
	}

	// if they are dates, they must had equal valueOf
	if (x instanceof Date)
	{
		return false;
	}

	// if they are strictly equal, they both need to be object at least
	if (!(x instanceof Object))
	{
		return false;
	}
	if (!(y instanceof Object))
	{
		return false;
	}

	// recursive object equality check
	var p = Object.keys(x);
	return Object.keys(y).every(function (i)
	{
		return p.indexOf(i) !== -1;
	}) &&
		p.every(function (i)
		{
			return IsEqual(x[i], y[i]);
		});
}

function GetCaptureMatches(string, regex, index)
{
	/// <summary>
	///     credit: http://stackoverflow.com/a/14210948/1919456 <br />
	/// Author: Ahmed Elsawalhy
	/// </summary>
	index || (index = 1); // default to the first capturing group

	var matches = [];
	var match;

	while (match = regex.exec(string))
	{
		matches.push(match[index]);
	}

	return matches;
}

function Repeat(str, count)
{
    /// <summary>
	/// credit: http://www.w3resource.com/javascript-exercises/javascript-string-exercise-21.php <br />
	/// Author: Ahmed Elsawalhy
    /// </summary>
	if ((count < 0) || (count === Infinity) || (count == null))
	{
		return ('Error in string or count.');
	}

	count = count | 0; // Floor count

	return new Array(count + 1).join(str);
}

//#region >>>>>>>>> Date helpers <<<<<<<<<<< //

function GetDateStringFromJson(date)
{
	if (!date)
	{
		return "";
	}

	var utcTime = parseInt(date.substr(6));

	return new Date(utcTime);
}

function CompareDate(x, y)
{
	/// <summary>
	///     Compare date x and y. Returns 1 if x &gt; y, -1 if x &lt; y, otherwise 0
	/// </summary>
	if (x.getYear() < y.getYear())
	{
		return 1;
	}
	else if (x.getYear() > y.getYear())
	{
		return -1;
	}

	// there is a tie at years, so see the months
	if (x.getMonth() < y.getMonth())
	{
		return 1;
	}
	else if (x.getMonth() > y.getMonth())
	{
		return -1;
	}

	// there is a tie at months, so see the days
	if (x.getDate() < y.getDate())
	{
		return 1;
	}
	else if (x.getDate() > y.getDate())
	{
		return -1;
	}

	// there is a tie in days, so check hours
	if (x.getHours() < y.getHours())
	{
		return 1;
	}
	else if (x.getHours() > y.getHours())
	{
		return -1;
	}

	// there is a tie in hours, so check minutes
	if (x.getMinutes() < y.getMinutes())
	{
		return 1;
	}
	else if (x.getMinutes() > y.getMinutes())
	{
		return -1;
	}

	// there is a tie in all, so return 0
	return 0;
}

//#endregion

function GetRequestObject()
{
	/// <summary>
	///     Internal use only!
	/// </summary>
	if (window.XMLHttpRequest)
	{
		return new window.XMLHttpRequest;
	}
	else
	{
		try
		{
			return new ActiveXObject("MSXML2.XMLHTTP.3.0");
		}
		catch (ex)
		{
			return null;
		}
	}
}

function HtmlEncode(value)
{
	/// <summary>
	///     credit: http://stackoverflow.com/a/7124052/1919456 <br />
	/// Author: Ahmed Elsawalhy
	/// </summary>
	return value
		.replace(/&/g, '&amp;')
		.replace(/"/g, '&quot;')
		.replace(/'/g, '&#39;')
		.replace(/</g, '&lt;')
		.replace(/>/g, '&gt;');
}

function IsHtml(text)
{
	return $('<div>').html(text).children().length > 0;
}

function GetFunctionName()
{
    /// <summary>
	/// credit: http://stackoverflow.com/a/1013370/1919456 <br />
	/// Author: Ahmed Elsawalhy
    /// </summary>
	var re = /function (.*?)\(/;
	var s = GetFunctionName.caller.toString();
	var m = re.exec(s);
	return m[1];
}

function GetElementDimensions(jqElement, maxWidth, jqScoped)
{
	var $ = jqScoped || this.$;

	var htmlOrg = jqElement.html();
	var htmlCalc = $('<span style="position:absolute;' +
		(maxWidth ? 'width:' + maxWidth + 'px;' : '') + '">' + htmlOrg + '</span>');
	$('body').append(htmlCalc);
	var width = htmlCalc.width();
	var height = htmlCalc.height();

	// make sure that there is no extra width because of short text
	htmlCalc.width($('body').width());

	if (htmlCalc.height() === height)
	{
		htmlCalc.css('width', '');
		width = htmlCalc.width();
	}

	htmlCalc.remove();

	return [width + 1, height];
}

//#region >>>>>>>>> Colour helpers <<<<<<<<<<< //

// credit: http://stackoverflow.com/a/13542669/1919456

function shadeColor2(color, percent)
{
	var f = parseInt(color.slice(1), 16), t = percent < 0 ? 0 : 255, p = percent < 0 ? percent * -1 : percent, R = f >> 16, G = f >> 8 & 0x00FF, B = f & 0x0000FF;
	return "#" + (0x1000000 + (Math.round((t - R) * p) + R) * 0x10000 + (Math.round((t - G) * p) + G) * 0x100 + (Math.round((t - B) * p) + B)).toString(16).slice(1);
}

function blendColors(c0, c1, p)
{
	var f = parseInt(c0.slice(1), 16), t = parseInt(c1.slice(1), 16), R1 = f >> 16, G1 = f >> 8 & 0x00FF, B1 = f & 0x0000FF, R2 = t >> 16, G2 = t >> 8 & 0x00FF, B2 = t & 0x0000FF;
	return "#" + (0x1000000 + (Math.round((R2 - R1) * p) + R1) * 0x10000 + (Math.round((G2 - G1) * p) + G1) * 0x100 + (Math.round((B2 - B1) * p) + B1)).toString(16).slice(1);
}

function shadeRGBColor(color, percent)
{
	var f = color.split(","), t = percent < 0 ? 0 : 255, p = percent < 0 ? percent * -1 : percent, R = parseInt(f[0].slice(4)), G = parseInt(f[1]), B = parseInt(f[2]);
	return "rgb(" + (Math.round((t - R) * p) + R) + "," + (Math.round((t - G) * p) + G) + "," + (Math.round((t - B) * p) + B) + ")";
}

function blendRGBColors(c0, c1, p)
{
	var f = c0.split(","), t = c1.split(","), R = parseInt(f[0].slice(4)), G = parseInt(f[1]), B = parseInt(f[2]);
	return "rgb(" + (Math.round((parseInt(t[0].slice(4)) - R) * p) + R) + "," + (Math.round((parseInt(t[1]) - G) * p) + G) + "," + (Math.round((parseInt(t[2]) - B) * p) + B) + ")";
}

function shade(color, percent)
{
	if (color.length > 7) return shadeRGBColor(color, percent);
	else return shadeColor2(color, percent);
}

function blend(color1, color2, percent)
{
	if (color1.length > 7) return blendRGBColors(color1, color2, percent);
	else return blendColors(color1, color2, percent);
}

// credit: http://stackoverflow.com/a/11868398/1919456
function IsDarkColour(colour)
{
	var r, g, b, f;

	if (colour.length > 7)
	{
		f = colour.split(",");
		r = parseInt(f[0].slice(4));
		g = parseInt(f[1]);
		b = parseInt(f[2]);
	}
	else
	{
		r = parseInt(colour.substr(1, 2), 16);
		g = parseInt(colour.substr(3, 2), 16);
		b = parseInt(colour.substr(5, 2), 16);
	}

	return ((r * 299) + (g * 587) + (b * 114)) / 1000 < 128;
}

//#endregion

//#endregion

////////////////////////////////////////////
//#region >>>>>>>>> Notify.js <<<<<<<<<<< //

var Notify = window.Notify || {};

Notify._notifications = [];
Notify._TimeStamp = null;
Notify._initialised = false;
Notify._crmFormHeaderId = "formHeaderContainer"; // This is probably the only thing "unsupported"
Notify._crmViewHeaderId = "crmContentPanel"; // And this, but it's cool

// message = (optional) what is displayed in the notification bar
// level = (optional) ERROR, WARNING, INFO, SUCCESS, QUESTION, or LOADING. If not included, no image will display
// uniqueId = (optional) unique ID for this notification
// buttons = (optional) array of objects, each object must have a 'text' attrbute, a 'callback' function attribute, and a 'type' attribute of 'link' or 'button'
// durationSeconds = (optional) after how long should the notification disappear
Notify.add = function(message, level, uniqueId, buttons, durationSeconds)
{
	if (!Notify._initialised)
	{
		var $notify = $("<div>", { id: "notifyWrapper" });
		$notify.append($("<div>", { id: "notify", class: "notify", size: "3", maxheight: "51" })
			.css("display", "block"));

		// Try get the form header
		var $header = $("#" + Notify._crmFormHeaderId);
		if ($header.length > 0)
		{
			$header.append($notify);
		}

		if ($header.length === 0)
		{
			$ = parent.$;

			// Try get the form header again (2015 SP1)
			$header = $("#" + Notify._crmFormHeaderId);
			if ($header.length > 0)
			{
				$header.append($notify);
			}

			// If not form header, might be a view, so try get the view header
			if ($header.length === 0)
			{
				$header = $("#" + Notify._crmViewHeaderId);
				if ($header.length > 0)
				{
					$header.prepend($notify);
				}
			}
		}

		if ($header.length > 0)
		{
			// Load the style sheet
			var baseUrl = Xrm.Page.context.getClientUrl();
			$("<link/>", { rel: "stylesheet", href: baseUrl + "/WebResources/mag_/css/notify.css" }).appendTo('head');

			Notify._initialised = true;
		}
		else
		{
			// Broken most likely from a rollup/update - just need to find the new header ID (hopefully)
			console.error("Notify: CRM header element: '" + Notify._crmHeaderId + "' does not exist.");
			return;
		}
	}

	// Accepts non-strings and undefined
	uniqueId = uniqueId ? (uniqueId + "").toLowerCase() : "";

	var notification = {
			id: uniqueId,
			severity: level,
			buttons: buttons
		};

	// Update or append the new notification
	var exists = false;
	for (var i = 0; i < Notify._notifications.length; i++)
	{
		if (Notify._notifications[i].id === uniqueId)
		{
			Notify._notifications[i] = notification;
			exists = true;
			break;
		}
	}
	if (!exists)
	{
		Notify._notifications.push(notification);
	}

	// Unhide the notify wrapper if this is the first notification
	$("#notifyWrapper").show();

	// If the element exists remove it before recreating it
	$("#notifyNotification_" + uniqueId).remove();

	// Create all the elements for this notification
	var $elem = $("<div>", { id: "notifyNotification_" + uniqueId, class: "notify-notification" }).hide()
		.prependTo($("#notify"));
	var $table = $("<table>", { cellpadding: "0", cellspacing: "0" }).css("width", "100%").appendTo($elem);
	var $tr = $("<tr>").appendTo($table);
	if (level && ["INFO", "WARNING", "ERROR", "SUCCESS", "QUESTION", "LOADING"].indexOf(level) !== -1)
	{
		var $imgTd = $("<td>", { valign: "top" }).css("width", "23px").appendTo($tr);
		var imgType = level === "ERROR"
						  ? "crit"
						  : level === "WARNING"
						  ? "warn"
						  : level === "INFO" ? "info" : level === "SUCCESS" ? "tick" : level === "QUESTION" ? "ques" : "load";
		var $img = $("<div>");
		$img.addClass("notify-image notify-image-" + imgType);
		$img.appendTo($imgTd);
	}
	var $textTd = $("<td>").appendTo($tr);
	var $close = $("<a>", { title: "Dismiss", class: "notify-close" }).click(function()
	{
		Notify.remove(uniqueId);
	});;
	$textTd.append($close);
	$textTd.append(message || "");
	if (buttons && buttons.length > 0)
	{
		for (var i = 0; i < buttons.length; i++)
		{
			var b = buttons[i];
			var $button = $("<a>", { class: b.type == "link" ? "notify-link" : "notify-button" }).click(b.callback);
			$button.append(b.text || "");
			$textTd.append($button);
		}
	}

	if (exists)
	{
		$elem.show();
	}
	else
	{
		$elem.slideDown(500);
	}

	// If there's a timeout specified, wait and then remove this notification
	if (durationSeconds && durationSeconds > 0)
	{
		var timeStamp = new Date();
		Notify._TimeStamp = timeStamp; // Timestamp prevents mutliple presses
		setTimeout(function()
		{
			if (timeStamp === Notify._TimeStamp)
			{
				Notify.remove(uniqueId);
			}
		}, durationSeconds * 1000);
	}

	setTimeout(function()
	{
		var evt = document.createEvent('UIEvents');
		evt.initUIEvent('resize', true, false, window, 0);
		top.dispatchEvent(evt);
	}, 800);
};

// uniqueId = (optional) the ID of the notification to remove. If ID is not specified, all notifications are cleared
Notify.remove = function(uniqueId)
{
	if (!Notify._initialised)
	{
		return;
	}

	// If no ID specified, remove all notifications
	if (uniqueId === null || uniqueId === undefined)
	{
		$("#notifyWrapper").slideUp(500, function()
		{
			for (var i = 0; i < Notify._notifications.length; i++)
			{
				$("#notifyNotification_" + Notify._notifications[i].id).remove();
			}

			Notify._notifications = [];
		});
	}
	else
	{
		// Accepts non-strings
		uniqueId = (uniqueId + "").toLowerCase();

		// Remove the notification
		var tempNotifications = [];
		for (var i = 0; i < Notify._notifications.length; i++)
		{
			if (Notify._notifications[i].id !== uniqueId)
			{
				tempNotifications.push(Notify._notifications[i]);
			}
		}
		Notify._notifications = tempNotifications;

		if (Notify._notifications.length === 0)
		{
			// If that was the last notification hide the notify wrapper
			$("#notifyWrapper").slideUp(500, function()
			{
				// Delete the notification once hidden
				$("#notifyNotification_" + uniqueId).remove();
			});
		}
		else
		{
			// Hide and Delete the element
			$("#notifyNotification_" + uniqueId).slideUp(500, function()
			{
				$(this).remove();
			});
		}
	}

	setTimeout(function()
	{
		var evt = document.createEvent('UIEvents');
		evt.initUIEvent('resize', true, false, window, 0);
		top.dispatchEvent(evt);
	}, 800);
};

Notify.clearNotifications = function()
{
	var notifications = [];

	Notify._notifications.forEach(function(e)
	{
		notifications.push(e.id);
	});

	notifications.forEach(function(e)
	{
		Notify.remove(e);
	});
};

//#endregion

/*
	* Date Format 1.2.3
	* (c) 2007-2009 Steven Levithan <stevenlevithan.com>
	* MIT license
	*
	* Includes enhancements by Scott Trenda <scott.trenda.net>
	* and Kris Kowal <cixar.com/~kris.kowal/>
	*
	* Accepts a date, a mask, or a date and a mask.
	* Returns a formatted version of the given date.
	* The date defaults to the current date/time.
	* The mask defaults to dateFormat.masks.default.
	*/

var dateFormat = function ()
{
	var token = /d{1,4}|m{1,4}|yy(?:yy)?|([HhMsTt])\1?|[LloSZ]|"[^"]*"|'[^']*'/g,
		timezone = /\b(?:[PMCEA][SDP]T|(?:Pacific|Mountain|Central|Eastern|Atlantic) (?:Standard|Daylight|Prevailing) Time|(?:GMT|UTC)(?:[-+]\d{4})?)\b/g,
		timezoneClip = /[^-+\dA-Z]/g,
		pad = function (val, len)
		{
			val = String(val);
			len = len || 2;
			while (val.length < len) val = "0" + val;
			return val;
		};

	// Regexes and supporting functions are cached through closure
	return function (date, mask, utc)
	{
		var dF = dateFormat;

		// You can't provide utc if you skip other args (use the "UTC:" mask prefix)
		if (arguments.length == 1 && Object.prototype.toString.call(date) == "[object String]" && !/\d/.test(date))
		{
			mask = date;
			date = undefined;
		}

		// Passing date through Date applies Date.parse, if necessary
		date = date ? new Date(date) : new Date;
		if (isNaN(date)) throw SyntaxError("invalid date");

		mask = String(dF.masks[mask] || mask || dF.masks["default"]);

		// Allow setting the utc argument via the mask
		if (mask.slice(0, 4) == "UTC:")
		{
			mask = mask.slice(4);
			utc = true;
		}

		var _ = utc ? "getUTC" : "get",
			d = date[_ + "Date"](),
			D = date[_ + "Day"](),
			m = date[_ + "Month"](),
			y = date[_ + "FullYear"](),
			H = date[_ + "Hours"](),
			M = date[_ + "Minutes"](),
			s = date[_ + "Seconds"](),
			L = date[_ + "Milliseconds"](),
			o = utc ? 0 : date.getTimezoneOffset(),
			flags = {
				d: d,
				dd: pad(d),
				ddd: dF.i18n.dayNames[D],
				dddd: dF.i18n.dayNames[D + 7],
				m: m + 1,
				mm: pad(m + 1),
				mmm: dF.i18n.monthNames[m],
				mmmm: dF.i18n.monthNames[m + 12],
				yy: String(y).slice(2),
				yyyy: y,
				h: H % 12 || 12,
				hh: pad(H % 12 || 12),
				H: H,
				HH: pad(H),
				M: M,
				MM: pad(M),
				s: s,
				ss: pad(s),
				l: pad(L, 3),
				L: pad(L > 99 ? Math.round(L / 10) : L),
				t: H < 12 ? "a" : "p",
				tt: H < 12 ? "am" : "pm",
				T: H < 12 ? "A" : "P",
				TT: H < 12 ? "AM" : "PM",
				Z: utc ? "UTC" : (String(date).match(timezone) || [""]).pop().replace(timezoneClip, ""),
				o: (o > 0 ? "-" : "+") + pad(Math.floor(Math.abs(o) / 60) * 100 + Math.abs(o) % 60, 4),
				S: ["th", "st", "nd", "rd"][d % 10 > 3 ? 0 : (d % 100 - d % 10 != 10) * d % 10]
			};

		return mask.replace(token, function ($0)
		{
			return $0 in flags ? flags[$0] : $0.slice(1, $0.length - 1);
		});
	};
}();

// Some common format strings
dateFormat.masks = {
	"default": "ddd mmm dd yyyy HH:MM:ss",
	shortDate: "m/d/yy",
	mediumDate: "mmm d, yyyy",
	longDate: "mmmm d, yyyy",
	fullDate: "dddd, mmmm d, yyyy",
	shortTime: "h:MM TT",
	mediumTime: "h:MM:ss TT",
	longTime: "h:MM:ss TT Z",
	isoDate: "yyyy-mm-dd",
	isoTime: "HH:MM:ss",
	isoDateTime: "yyyy-mm-dd'T'HH:MM:ss",
	isoUtcDateTime: "UTC:yyyy-mm-dd'T'HH:MM:ss'Z'"
};

// Internationalization strings
dateFormat.i18n = {
	dayNames: [
		"Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat",
		"Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"
	],
	monthNames: [
		"Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec",
		"January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"
	]
};
