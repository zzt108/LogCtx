# LogCtx - NLog-Native Structured Logging

NLog-native structured logging library with context management using `ILogger<T>` and `BeginScope`.

## Features

- ✅ Native `ILogger<T>` integration (no custom abstractions)
- ✅ Automatic CallerInfo capture (file, method, line)
- ✅ Stack trace filtering (excludes framework noise)
- ✅ SEQ structured logging support
- ✅ Fluent Props API for context properties
- ✅ Nested scope support

## Quick Start

### 1. Install NuGet Package

```
dotnet add package LogCtx
```

### 2. Configure NLog

Add `NLog.config` to your project:

```
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd">
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

```
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
        using (_logger.SetContext(new Props()
            .Add("OrderId", orderId)
            .Add("CustomerId", customerId)))
        {
            _logger.LogInformation("Processing order");
            // All logs within this scope include OrderId and CustomerId
        }
    }
}
```

## SEQ Setup

### Install SEQ

```
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

```
IDisposable SetContext(this ILogger logger, Props? props = null)
```

Sets logging context with automatic CallerInfo capture. Use with `using` statement.

### SetOperationContext

```
IDisposable SetOperationContext(this ILogger logger, string operationName, params (string, object)[] properties)
```

Convenience method for operation-scoped logging.

### Props

Fluent API for building context properties:

```
var props = new Props()
    .Add("Key1", "Value1")
    .Add("Key2", 42)
    .AddJson("ComplexObject", myObject);
```

## Testing

### Run Unit Tests

```
dotnet test
```

### Run SEQ Integration Tests

Requires SEQ running at http://localhost:5341:

```
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
```

***

## File Summary

| File | Type | Purpose |
|------|------|---------|
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
- Logs with `CTX_STRACE` property containing file::method::line
- `UserId`, `Action`, `Operation` properties from Props
- Structured queries work: `UserId = 12345`

## Docs

- API Reference: .doc/API-REFERENCE.md
- Troubleshooting: .doc/TROUBLESHOOTING.md
- Migration: .doc/MIGRATION.md
- Migration examples: .doc/MIGRATION-EXAMPLES.md
- MAUI samples: .doc/MAUI-SAMPLES.md
