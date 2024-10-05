using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace LogCtxNameSpace;
public static class LogCtx
{
    public const string FILE = "CTX_FILE";
    public const string LINE = "CTX_LINE";
    public const string METHOD = "CTX_METHOD";
    public const string SRC = "CTX_SRC";

    public const string STRACE = "CTX_STRACE";

    public static bool CanLog = true;

    //public static ILogger? Logger;
    private static IScopeContext? _scopeContext;

    public static void Init(IScopeContext scopeContext)
    {
        _scopeContext = scopeContext;
    }

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

    public static T FromJson<T>(string value) => JsonConvert.DeserializeObject<T>(value);

    public static string Link(
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        var fileName = Path.GetFileNameWithoutExtension(sourceFilePath);
        return $"{sourceFilePath}({sourceLineNumber}):WT@F";
    }

    /// <summary>
    /// Sets the scope context properties.
    /// </summary>
    /// <param name="scopeContextProps">The scope context properties.</param>
    /// <param name="methodNameLogLevel">The method name log level.</param>
    /// <param name="memberName">Name of the member.</param>
    /// <param name="sourceFilePath">The source file path.</param>
    /// <param name="sourceLineNumber">The source line number.</param>
    /// <returns>The scope context properties.</returns>
    public static Props Set(
        Props scopeContextProps = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        _scopeContext.Clear();

        var fileName = Path.GetFileNameWithoutExtension(sourceFilePath);
        var methodName = memberName;
        var strace = $"{fileName}::{methodName}::{sourceLineNumber}\r\n";
        var strace2 = $"{fileName}::{methodName}::{sourceLineNumber}\r\n";
        var tr = new System.Diagnostics.StackTrace();
        var sr = tr.ToString().Split('\n');

        foreach (var frame in sr)
        {
            if (!frame.Trim().StartsWith("at System.") &&
                !frame.Trim().StartsWith("at NUnit.") &&
                !frame.Trim().StartsWith("at NLog.") &&
                !frame.Trim().StartsWith("at TechTalk.") &&
                !(frame == sr[0])
                )
            {
                strace2 += $"--{frame}\n";
            }
        }

        _scopeContext.PushProperty(STRACE, strace2);

        scopeContextProps ??= new Props();
        scopeContextProps.Remove(STRACE);
        scopeContextProps.Add(STRACE, strace2);

        foreach (var key in scopeContextProps.Keys)
        {
            _scopeContext.PushProperty(key, scopeContextProps[key]?.ToString());
        }

        return scopeContextProps;
    }

    public static string Src(
        this string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        var fileName = Path.GetFileNameWithoutExtension(sourceFilePath);
        var methodName = memberName;

        return $"{fileName}.{methodName}.{sourceLineNumber}";
    }

    public class Props : Dictionary<string, object>, IDisposable
    {
        private bool _disposedValue;

        public Props()
        {
        }

        public Props(params object[] args)
        {
            int i = 0;
            foreach (var item in args)
            {
                Add($"P{i:x2}", item.AsJson(true));
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    _scopeContext.Clear();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _disposedValue = true;
            }
        }

        public Props AddJson(string key, object value)
        {
            Add(key, JsonConvert.SerializeObject(value));
            return this;
        }

        public new Props Add(string key, object? value)
        {
            if (ContainsKey(key))
            {
                Remove(key);
            }

            if (value == null)
            {
                base.Add(key, "null value");
            }
            else
            {
                base.Add(key, value);
            }

            return this;
        }

        public new Props Clear()
        {
            base.Clear();
            return this;
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~Props()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
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
