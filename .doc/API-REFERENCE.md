# API Reference (LogCtxShared)

This document describes the public API surface of the NLog-native structured logging helpers.

## Namespace
- `LogCtxShared`

## NLogContextExtensions

### SetContext
**Signature**
- `IDisposable SetContext(this ILogger logger, Props? props = null, ...)`

**Purpose**
- Creates a scope (`BeginScope`) that carries structured properties for all logs within the `using` block.
- Automatically enriches the scope with CallerInfo-based source context and (by default) a filtered stack trace property.

**Typical usage**
```

using LogCtxShared;
using Microsoft.Extensions.Logging;

var props = new Props()
.Add("OrderId", orderId)
.Add("CustomerId", customerId);

using (logger.SetContext(props))
{
logger.LogInformation("Processing {OrderId} for {CustomerId}", orderId, customerId);
}

```

### SetOperationContext
**Signature**
- `IDisposable SetOperationContext(this ILogger logger, string operationName, params (string Key, object? Value)[] properties)`

**Purpose**
- Convenience scope helper for operation-scoped correlation (operation name + additional structured properties).

**Typical usage**
```

using (logger.SetOperationContext(
operationName: "ProcessOrder",
("OrderId", orderId),
("CustomerId", customerId)))
{
logger.LogInformation("Operation started");
}

```

## Props

`Props` is a fluent dictionary used as the scope state passed into `BeginScope`.

**Key behaviors**
- Fluent `.Add(key, value)` for easy chaining.
- Dictionary-like access (e.g., `props["UserId"]`).
- `Clear()` for reuse in complex workflows.
- `AddJson(key, obj)` for optional JSON serialization when you really need to store a complex object as a single property (note: this is less query-friendly than scalar keys).

**Typical usage**
```

var props = new Props()
.Add("UserId", userId)
.Add("Action", "ButtonClick");

using (logger.SetContext(props))
{
logger.LogInformation("User action completed");
}

```

## LogContextKeys

`LogContextKeys` defines standard property names so logs are queryable consistently in SEQ.

**Known keys**
- `LogContextKeys.FILE` => `CTXFILE`
- `LogContextKeys.LINE` => `CTXLINE`
- `LogContextKeys.METHOD` => `CTXMETHOD`
- `LogContextKeys.SRC` => `CTXSRC`
- `LogContextKeys.STRACE` => `CTXSTRACE`

## SourceContext

Utilities for CallerInfo capture and stack trace formatting.

### BuildSource
- Builds a compact source location string (file/method/line).

### BuildStackTrace
- Builds a filtered stack trace string excluding framework/test/logging infrastructure frames.
- Used by `SetContext` to populate `CTXSTRACE` (unless already present).

## MauiSetup

Helpers for wiring NLog into `Microsoft.Extensions.Logging`.

### AddNLogLogging
**Signature**
- `ILoggingBuilder AddNLogLogging(this ILoggingBuilder logging, IConfiguration? configuration = null, string? seqUrl = null, string? apiKey = null, string nlogConfigFileName = "NLog.config")`

**Notes**
- Enables scope capture so `SetContext` properties flow to targets (e.g., SEQ).
- Loads `NLog.config` safely (fallback behavior if config missing).

**MAUI usage**
```

builder.Logging.AddNLogLogging(
builder.Configuration,
seqUrl: "http://10.0.2.2:5341",
apiKey: null,
nlogConfigFileName: "NLog.config");

```

### CreateLoggerFactory
**Signature**
- `ILoggerFactory CreateLoggerFactory(IConfiguration? configuration = null, string? seqUrl = null, string? apiKey = null, string nlogConfigFileName = "NLog.config")`

**Usage**
- Useful for tests / console apps that are not using the MAUI host builder directly.
