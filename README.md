# LogCtx

`LogCtx` is a structured logging helper built on top of `Microsoft.Extensions.Logging`. It uses `BeginScope` under the hood and adds source/caller context automatically through `SetContext()`. This is the current, real usage model in the codebase.

## Core rule

In normal app code, use:

- `ILogger<T>` injection
- `logger.SetContext()` for a scope
- optional `.Add(...)` calls for structured properties
- normal `LogInformation`, `LogWarning`, `LogError`, etc. inside that scope

## Important clarifications

### `SetContext()` is useful even by itself

This is valid and meaningful:

```csharp
using var ctx = logger.SetContext();
logger.LogInformation("Application started");
```

Even without extra properties, `SetContext()` captures caller/source information such as source file, member, line, and stack-trace-style context metadata.

### `MauiSetup` is only a bootstrap helper name

`LogCtxShared.MauiSetup` may be used from non-MAUI apps too. In the current codebase it is used from an Avalonia desktop app to create the logger factory.

Do not infer a MAUI-only usage rule from the class name.

## Real usage patterns

### 1. Bootstrap logging once

```csharp
using Microsoft.Extensions.Logging;
using LogCtxShared;

var loggerFactory = MauiSetup.CreateLoggerFactory(
    configuration: null,
    seqUrl: "http://localhost:5341",
    apiKey: null,
    nlogConfigFileName: "NLog.config");

var logger = loggerFactory.CreateLogger<Program>();

using var ctx = logger.SetContext();
logger.LogInformation("Starting app");
```

### 2. Service logging

```csharp
public sealed class LogWatcherService
{
    private readonly ILogger<LogWatcherService> _logger;
    private readonly string _logPath;

    public LogWatcherService(ILogger<LogWatcherService> logger, string logPath)
    {
        _logger = logger;
        _logPath = logPath;
    }

    public void Start()
    {
        using var ctx = _logger.SetContext()
            .Add("LogPath", _logPath);

        _logger.LogInformation("Starting log watcher");
    }
}
```

### 3. ViewModel / UI operation logging

```csharp
public async Task CopyToClipboardAsync(string text)
{
    using var ctx = _logger.SetContext()
        .Add("Operation", "CopyToClipboard")
        .Add("TextLength", text.Length);

    _logger.LogInformation("Copying text to clipboard");
    await _clipboard.SetTextAsync(text);
    _logger.LogInformation("Clipboard copy complete");
}
```

### 4. Exception logging

```csharp
using var ctx = _logger.SetContext()
    .Add("Operation", "WatchLoop")
    .Add("LogPath", _logPath);

try
{
    await RunAsync(ct);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Watch loop failed");
    throw;
}
```

## What to avoid

### Avoid direct `NLog.*` usage in normal app code

The main project should think in terms of `ILogger<T>` + `LogCtx` scopes.

Direct NLog references are acceptable only for bootstrap/configuration when truly needed.

### Avoid `new Props(...)` in app code

Normal application code should start from `logger.SetContext()`.

### Avoid giant long-lived scopes

Prefer short-lived scopes tied to real operations such as:

- startup
- file watcher start
- watch loop
- payload parse
- clipboard operation
- one specific command or action

## Current mental model

Use this sentence as the default rule:

> Open a context at the operation boundary, add a few stable structured properties if they help, and log normally inside that scope.

## Minimal checklist for AI agents

- Inject `ILogger<T>`.
- Call `SetContext()` at the start of each meaningful operation.
- Add `.Add(...)` only for useful structured properties.
- Use `LogError(ex, ...)` for exceptions.
- Do not spread raw `NLog` APIs through application code.
