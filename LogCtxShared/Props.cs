using Newtonsoft.Json;

namespace LogCtxShared;

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
            Add($"P{i++:x2}", item.AsJson(true));
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
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
