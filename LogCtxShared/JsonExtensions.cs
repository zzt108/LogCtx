using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace LogCtxShared
{
    public static class JsonExtensions
    {
        public static string AsJson(this object obj, bool indented = false)
        {
            if (indented)
            {
                return $"{JsonConvert.SerializeObject(obj, Formatting.Indented)}\n";
            }
            else
            {
                return JsonConvert.SerializeObject(obj);
            }
        }

        /// <summary>
        /// Converts to full PlantUml json diagram. Do not use inside another PlantUml diagram!
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string AsJsonDiagram(this object obj) => $"@startjson {obj.GetType().Name}\n{JsonConvert.SerializeObject(obj, Formatting.Indented)}\n@endjson\n";

        /// <summary>
        /// Converts to embedded json in a PlantUml diagram.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string AsJsonEmbedded(this object obj) => $"json \"{obj.GetType().Name}\" as J{{\n{JsonConvert.SerializeObject(obj, Formatting.Indented)}\n}}\n";

        public static T? FromJson<T>(string value) => JsonConvert.DeserializeObject<T>(value);

        public static string Link(
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            var fileName = Path.GetFileNameWithoutExtension(sourceFilePath);
            return $"{sourceFilePath}({sourceLineNumber}):WT@F";
        }

    }
}

/* SEQ Signals for low level log
{
"Title": "1 Verbose",
"Description": "NLog",
"Filters": [
{
"Description": null,
"DescriptionIsExcluded": false,
"Filter": "@Level = 'Verbose' ci",
"FilterNonStrict": "@Level == 'Verbose' ci"
}
],
"Columns": [],
"IsProtected": false,
"Grouping": "Explicit",
"ExplicitGroupName": "@Level",
"OwnerId": null,
"Id": "signal-36",
"Links": {
"Self": "api/signals/signal-36?version=6",
"Group": "api/signals/resources"
}
}

{
"Title": "2 Debug",
"Description": "SeriLog",
"Filters": [
{
"Description": null,
"DescriptionIsExcluded": false,
"Filter": "@Level = 'Debug' ci",
"FilterNonStrict": "@Level == 'Debug' ci"
}
],
"Columns": [],
"IsProtected": false,
"Grouping": "Explicit",
"ExplicitGroupName": "@Level",
"OwnerId": null,
"Id": "signal-37",
"Links": {
"Self": "api/signals/signal-37?version=5",
"Group": "api/signals/resources"
}
}

{
"Title": "3 Information",
"Description": "Automatically created",
"Filters": [
{
  "Description": null,
  "DescriptionIsExcluded": false,
  "Filter": "@Level in ['Information', 'Info'] ci",
  "FilterNonStrict": "@Level in ['Information', 'Info'] ci"
}
],
"Columns": [],
"IsProtected": false,
"Grouping": "Explicit",
"ExplicitGroupName": "@Level",
"OwnerId": null,
"Id": "signal-195",
"Links": {
"Self": "api/signals/signal-195?version=4",
"Group": "api/signals/resources"
}
}

{
"Title": "4 Warnings",
"Description": "Automatically created",
"Filters": [
{
  "Description": null,
  "DescriptionIsExcluded": false,
  "Filter": "@Level in ['w', 'wa', 'war', 'wrn', 'warn', 'warning'] ci",
  "FilterNonStrict": null
}
],
"Columns": [],
"IsProtected": false,
"Grouping": "Explicit",
"ExplicitGroupName": "@Level",
"OwnerId": null,
"Id": "signal-m33302",
"Links": {
"Self": "api/signals/signal-m33302?version=2",
"Group": "api/signals/resources"
}
}

{
"Title": "5 Errors",
"Description": "NLog",
"Filters": [
{
  "Description": null,
  "DescriptionIsExcluded": false,
  "Filter": "@Level in ['e', 'er', 'err', 'eror', 'erro', 'error'] ci",
  "FilterNonStrict": "@Level in ['e', 'er', 'err', 'eror', 'erro', 'error'] ci"
}
],
"Columns": [],
"IsProtected": false,
"Grouping": "Explicit",
"ExplicitGroupName": "@Level",
"OwnerId": null,
"Id": "signal-196",
"Links": {
"Self": "api/signals/signal-196?version=1",
"Group": "api/signals/resources"
}
}

{
"Title": "6 Fatal",
"Description": "NLog",
"Filters": [
{
  "Description": null,
  "DescriptionIsExcluded": false,
  "Filter": "@Level in ['f', 'fa', 'fat', 'ftl', 'fata', 'fatl', 'Fatal', 'c', 'cr', 'cri', 'crt', 'crit', 'critical', 'alert', 'emerg', 'panic'] ci",
  "FilterNonStrict": "@Level in ['f', 'fa', 'fat', 'ftl', 'fata', 'fatl', 'Fatal', 'c', 'cr', 'cri', 'crt', 'crit', 'critical', 'alert', 'emerg', 'panic'] ci"
}
],
"Columns": [],
"IsProtected": false,
"Grouping": "Explicit",
"ExplicitGroupName": "@Level",
"OwnerId": null,
"Id": "signal-196",
"Links": {
"Self": "api/signals/signal-196?version=1",
"Group": "api/signals/resources"
}
}

{
"Title": "Exceptions",
"Description": "Automatically created",
"Filters": [
{
  "Description": null,
  "DescriptionIsExcluded": false,
  "Filter": "@Exception is not null",
  "FilterNonStrict": null
}
],
"Columns": [],
"IsProtected": false,
"Grouping": "Inferred",
"ExplicitGroupName": null,
"OwnerId": null,
"Id": "signal-m33303",
"Links": {
"Self": "api/signals/signal-m33303?version=1",
"Group": "api/signals/resources"
}
}
*/
