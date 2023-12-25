using System;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Yagasoft.Libraries.Common;

namespace Yagasoft.Libraries.Plugins.Messages.Code
{
    public static class CodeGenerator
    {
	    public static string GenerateCode(CustomMessage customMessage, IOrganizationService service, ILogger log)
	    {
		    var source = GetStringValue(customMessage.Source);
		    var module = GetStringValue(customMessage.Module);
		    var category = GetStringValue(customMessage.Category);

		    var idBase = $"0x{source}{module}{category}";

		    log.Log($"Retrieving latest number for base '{idBase}' ...");
		    var latestId = service.RetrieveMultiple(
			    new FetchExpression(
				    $"""
<fetch top='1' no-lock='true' >
  <entity name='ys_custommessage' >
    <attribute name='ys_name' />
    <filter>
      <condition attribute='ys_sourcevalue' operator='eq' value='1{source}' />
      <condition attribute='ys_modulevalue' operator='eq' value='1{module}' />
      <condition attribute='ys_categoryvalue' operator='eq' value='1{category}' />
      <condition attribute='ys_custommessageid' operator='neq' value='{customMessage.Id}' />
    </filter>
    <order attribute='ys_name' descending='true' />
  </entity>
</fetch>
"""))
			    .Entities.FirstOrDefault()?.ToEntity<CustomMessage>().ID;
		    log.Log($"Found '{latestId}'.");

		    latestId ??= $"{idBase}000";

		    if (!int.TryParse(latestId.Replace(idBase, ""), out var latestIndex))
		    {
			    throw new InvalidPluginExecutionException("Failed to retrieve latest ID index.");
		    }

		    var nextId = (++latestIndex).ToString().PadLeft(3, '0');
		    log.Log($"Next index: {nextId}.");

		    var code = $"{idBase}{nextId}";
		    log.Log($"Next ID: {code}.");

		    return code;
	    }

	    private static string GetStringValue(Enum @enum)
	    {
		    @enum.Require(nameof(@enum));
		    return Convert.ToInt32(@enum).ToString().Substring(1);
	    }
    }
}
