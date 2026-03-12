# LogCtx Migration Guide

This guide removes legacy patterns and replaces them with the current recommended model.

## Migrate to this

- `ILogger<T>` in constructors
- `logger.SetContext()` at operation boundaries
- optional `.Add(...)` for stable structured properties
- normal MEL logging methods inside the scope

## Replace these legacy patterns

### 1. Raw console logging

Before:

```csharp
Console.WriteLine("Watcher failed");
```

After:

```csharp
using var ctx = _logger.SetContext()
    .Add("Operation", "WatchLoop");

_logger.LogError(ex, "Watcher failed");
```

### 2. Direct raw NLog usage in app code

Before:

```csharp
using NLog;
var logger = LogManager.GetCurrentClassLogger();
logger.Info("Started");
```

After:

```csharp
var logger = loggerFactory.CreateLogger<MyType>();
using var ctx = logger.SetContext();
logger.LogInformation("Started");
```

### 3. Manual `Props` construction

Before:

```csharp
var props = new Props(logger, null, null, null, 0);
props.Add("UserId", userId);
```

After:

```csharp
using var ctx = logger.SetContext()
    .Add("UserId", userId);
```

### 4. One giant application-wide scope

Before:

```csharp
using var ctx = logger.SetContext();
RunEntireApplication();
```

After:

```csharp
using var ctx = logger.SetContext();
logger.LogInformation("Application starting");
```

Then create fresh scopes inside meaningful services and operations.

## Current recommended migration targets

### Program bootstrap

- Create one logger factory.
- Create typed loggers from it.
- Use `SetContext()` for startup logs.
- Do not spread bootstrap-only concerns into normal service code.

### Services

- Inject `ILogger<T>`.
- Replace `Console.WriteLine` with structured logs.
- Add useful stable properties such as `LogPath`, `Operation`, `PayloadLength`, `ItemCount`.

### ViewModels

- Inject `ILogger<T>`.
- Log commands and user-triggered operations with small, focused scopes.

## Reality check

`SetContext()` alone is already valuable because it captures caller/source context.

Do not force dummy `.Add(...)` calls just to justify using it.
