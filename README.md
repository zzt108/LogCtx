# LogCtx - NLog-Native Structured Logging

NLog-native structured logging library with context management using `ILogger<T>` and `BeginScope`.

## Features

- ✅ Native `ILogger<T>` integration (no custom abstractions)
- ✅ Automatic CallerInfo capture (file, method, line)
- ✅ Stack trace filtering (excludes framework noise)
- ✅ SEQ structured logging support
- ✅ Fluent Props API for context properties
- ✅ Nested scope support
- ✅ Thread-safe Props based on ConcurrentDictionary

## Quick Start

### 1. Install NuGet Package

```bash
dotnet add package LogCtx
```


### 2. Configure NLog

Add `NLog.config` to your project:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
      autoReload="true"
      throwConfigExceptions="true">

  <variable name="seqUrl" value="http://localhost:5341" />

  <targets async="true">
    <target xsi:type="Seq" name="seq" serverUrl="${var:seqUrl}" />
  </targets>

  <rules>
    <logger name="*" minlevel="Trace" writeTo="seq" />
  </rules>
</nlog>
```


### 3. Use in Code

```csharp
using Microsoft.Extensions.Logging;
using LogCtxShared;

public class MyService
{
    private readonly ILogger<MyService> _logger;

    public MyService(ILogger<MyService> logger)
    {
        _logger = logger;
    }

    public void ProcessOrder(int orderId, int customerId)
    {
        using Props p = _logger.SetContext()
            .Add("OrderId", orderId)
            .Add("CustomerId", customerId);
        {
            _logger.LogInformation("Processing order");
            // All logs within this scope include OrderId, CustomerId and CTXSTRACE
        }
    }
}
```


## SEQ Setup

### Install SEQ

```bash
# Windows (via Chocolatey)
choco install seq

# macOS/Linux (via Docker)
docker run -d --name seq -e ACCEPT_EULA=Y -p 5341:80 datalust/seq:latest
```


### Access SEQ UI

- **URL:** http://localhost:5341
- **Default:** No authentication required


### Verify Logs in SEQ

1. Run your application
2. Open http://localhost:5341
3. Filter by `CTX_STRACE` property to see context logs
4. Use queries like: `OrderId = 123` or `Operation = "ProcessOrder"`

## API Reference

### SetContext

```csharp
Props SetContext(this ILogger logger,
    string memberName = "",
    string sourceFilePath = "",
    int sourceLineNumber = 0);
```

Extension method with CallerInfo attributes (in real code they are `[CallerMemberName]`, `[CallerFilePath]`, `[CallerLineNumber]`).

Creates a new `Props` scope with automatic CallerInfo capture. Use with `using`:

```csharp
using Props p = logger.SetContext()
    .Add("Key", "Value");
{
    logger.LogInformation("Message");
}
```


### SetContext (nested)

```csharp
Props SetContext(this ILogger logger, Props parent,
    string memberName = "",
    string sourceFilePath = "",
    int sourceLineNumber = 0);
```

Creates a nested context that **inherits** properties from `parent`, then disposes the parent scope.

```csharp
using Props p = logger.SetContext()
    .Add("userId", 123);

p = logger.SetContext(p)
    .Add("action", "login");

logger.LogInformation("Has userId and action");
```


### SetOperationContext

```csharp
IDisposable SetOperationContext(
    this ILogger logger,
    string operationName,
    params (string key, object value)[] properties);
```

Convenience method for operation-scoped logging.

```csharp
using (logger.SetOperationContext(
    "ProcessOrder",
    ("OrderId", orderId),
    ("CustomerId", customerId)))
{
    logger.LogInformation("Processing order");
}
```


### Props

Fluent, thread-safe API for building context properties:

```csharp
// Created internally by SetContext() – don't call new Props() directly in app code
using Props p = logger.SetContext()
    .Add("Key1", "Value1")
    .Add("Key2", 42)
    .AddJson("ComplexObject", myObject);
```

- Based on `ConcurrentDictionary<string, object>`
- Thread-safe Add / indexer
- Recreates scope when properties change so NLog sees updates


## Testing

### Run Unit Tests

```bash
dotnet test
```


### Run SEQ Integration Tests

Requires SEQ running at http://localhost:5341:

```bash
dotnet test --filter Category=Integration
```


## Context Keys

Standard context property keys (available in `LogContextKeys`):

- `CTX_FILE` - Source file name
- `CTX_LINE` - Source line number
- `CTX_METHOD` - Method name
- `CTX_SRC` - Compact source location
- `CTX_STRACE` - Filtered stack trace


## License

MIT

***

## File Summary

| File | Type | Purpose |
| :-- | :-- | :-- |
| LogCtx\NLog.config | ✅ NEW | SEQ + Console + File targets configuration |
| LogCtx\NLog.Development.config | ✅ NEW | Development-specific overrides |
| LogCtx\appsettings.json | ✅ NEW | SEQ connection settings |
| LogCtxShared.projitems | 🔄 MODIFY | Add NLog.Targets.Seq package |
| LogCtx.csproj | 🔄 MODIFY | Copy config files to output |
| Tests\Tests.csproj | 🔄 MODIFY | Add NLog.Targets.Seq for integration tests |
| Tests\Tests\SeqIntegrationTests.cs | ✅ NEW | Verify SEQ connectivity and context logging |
| README.md | ✅ NEW | Setup and usage documentation |


***

## Testing SEQ Integration

### 1. Start SEQ

```bash
# Docker (easiest)
docker run -d --name seq -e ACCEPT_EULA=Y -p 5341:80 datalust/seq:latest

# Or Windows service (if installed)
# SEQ runs automatically after installation
```


### 2. Run Integration Tests

```bash
cd C:\Git\LogCtx
dotnet test --filter "Category=Integration"
```


### 3. Verify in SEQ UI

Open http://localhost:5341 and you should see:

- Logs with `CTX_STRACE` property containing file.method.line
- `UserId`, `Action`, `Operation` properties from Props
- Structured queries work: `UserId = 12345`


## Docs

- API Reference: .doc/API-REFERENCE-v2.md
- Troubleshooting: .doc/TROUBLESHOOTING.md
- Migration: .doc/MIGRATION-GUIDE-v2.md
- Migration examples: .doc/MIGRATION-EXAMPLES.md
- MAUI samples: .doc/MAUI-SAMPLES.md
