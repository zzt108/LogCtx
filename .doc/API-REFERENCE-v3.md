# LogCtx API Reference

This file documents the current intended usage, not historical experiments.

## Primary API

### `SetContext()`

```csharp
Props SetContext(this ILogger logger,
    string memberName = "",
    string sourceFilePath = "",
    int sourceLineNumber = 0)
```

Creates a logging scope and automatically captures caller/source context.

Use it at the start of a method or operation:

```csharp
using var ctx = logger.SetContext();
logger.LogInformation("Operation started");
```

### `SetContext().Add(...)`

Adds structured properties to the current scope.

```csharp
using var ctx = logger.SetContext()
    .Add("Operation", "Import")
    .Add("ItemCount", itemCount);

logger.LogInformation("Running import");
```

### `SetContext(parent)`

```csharp
Props SetContext(this ILogger logger, Props parent,
    string memberName = "",
    string sourceFilePath = "",
    int sourceLineNumber = 0)
```

Creates a nested context that inherits the parent properties.

```csharp
using Props ctx = logger.SetContext()
    .Add("LogPath", logPath);

ctx = logger.SetContext(ctx)
    .Add("Operation", "WatchLoop");

logger.LogInformation("Now running inside nested context");
```

Use this only when you really need to extend an existing context. In most application code, a fresh `SetContext()` is enough.

### `SetOperationContext(...)`

```csharp
IDisposable SetOperationContext(
    this ILogger logger,
    string operationName,
    params (string key, object value)[] properties)
```

Convenience API for short operation-scoped logging.

```csharp
using var op = logger.SetOperationContext(
    "ProcessTurn",
    ("Turn", turnNumber),
    ("LogPath", logPath));

logger.LogInformation("Processing turn");
```

Useful, but not required. The main default pattern is still `SetContext()`.

## `Props`

`Props` is the scope/property object returned by `SetContext()`.

### Supported usage

```csharp
using var ctx = logger.SetContext()
    .Add("Key1", "Value1")
    .Add("Key2", 42);
```

### Do not do this in app code

```csharp
var p = new Props(...);
```

That may exist in tests or internal library scenarios, but normal application code should not construct `Props` directly.

## Structured logging examples

### Service method

```csharp
public void Start()
{
    using var ctx = _logger.SetContext()
        .Add("LogPath", _logPath);

    _logger.LogInformation("Starting log watcher");
}
```

### Background loop

```csharp
private async Task WatchLoop(CancellationToken ct)
{
    using var ctx = _logger.SetContext()
        .Add("LogPath", _logPath)
        .Add("Operation", "WatchLoop");

    _logger.LogDebug("Entering watch loop");
}
```

### UI command

```csharp
private async Task CopyToClipboard(string text)
{
    using var ctx = _logger.SetContext()
        .Add("Operation", "CopyToClipboard")
        .Add("TextLength", text.Length);

    _logger.LogInformation("Copying text to clipboard");
}
```

## Bootstrap

Bootstrap is separate from runtime usage.

A logger factory can be created via:

```csharp
var loggerFactory = MauiSetup.CreateLoggerFactory(
    configuration: null,
    seqUrl: "http://localhost:5341",
    apiKey: null,
    nlogConfigFileName: "NLog.config");
```

This is valid in Avalonia/Desktop apps too. The `MauiSetup` name is historical.
