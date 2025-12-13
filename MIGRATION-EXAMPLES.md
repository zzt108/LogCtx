# LogCtx → NLog-Native Migration Examples

This file contains practical before/after examples for migrating from legacy LogCtx patterns to NLog-native logging with `ILogger<T>` and scope-based context.

## 1) DI injection

### Before (legacy)
```

public sealed class OrderService
{
private readonly ILogCtxLogger _logger;

    public OrderService(ILogCtxLogger logger)
    {
        _logger = logger;
    }
    }

```

### After (NLog-native)
```

using Microsoft.Extensions.Logging;

public sealed class OrderService
{
private readonly ILogger<OrderService> _logger;

    public OrderService(ILogger<OrderService> logger)
    {
        _logger = logger;
    }
    }

```

## 2) Scoped context (“Set pattern”)

### Before (legacy)
```

var props = new Props()
.Add("OrderId", orderId)
.Add("CustomerId", customerId);

LogCtx.Set(props);
_logger.Info("Processing order");

```

### After (NLog-native)
```

using LogCtxShared;
using Microsoft.Extensions.Logging;

var props = new Props()
.Add("OrderId", orderId)
.Add("CustomerId", customerId);

using (_logger.SetContext(props))
{
_logger.LogInformation("Processing order");
}

```

## 3) Operation-scoped logging

### Before (legacy)
```

LogCtx.Set(new Props().Add("Operation", "ProcessOrder"));
_logger.Info("Start");

```

### After (NLog-native)
```

using LogCtxShared;
using Microsoft.Extensions.Logging;

using (_logger.SetOperationContext(
"ProcessOrder",
("OrderId", orderId),
("CustomerId", customerId)))
{
_logger.LogInformation("Start");
}

```

## 4) Structured message templates

### Before (legacy)
```

_logger.Info("Processing {OrderId} for {CustomerId}", orderId, customerId);

```

### After (NLog-native)
```

using Microsoft.Extensions.Logging;

_logger.LogInformation("Processing {OrderId} for {CustomerId}", orderId, customerId);

```

## 5) Exceptions with context

### Before (legacy)
```

LogCtx.Set(new Props().Add("UserId", userId));

try
{
DoWork();
}
catch (Exception ex)
{
_logger.Error(ex, "Work failed");
}

```

### After (NLog-native)
```

using LogCtxShared;
using Microsoft.Extensions.Logging;

using (_logger.SetContext(new Props().Add("UserId", userId)))
{
try
{
DoWork();
}
catch (Exception ex)
{
_logger.LogError(ex, "Work failed");
}
}

```

## 6) Nested scopes (outer + inner)

### After (NLog-native)
```

using LogCtxShared;
using Microsoft.Extensions.Logging;

using (_logger.SetContext(new Props().Add("Operation", "Import")))
{
_logger.LogInformation("Import started");

    foreach (var record in records)
    {
        using (_logger.SetContext(new Props().Add("RecordId", record.Id)))
        {
            _logger.LogInformation("Processing record");
        }
    }
    
    _logger.LogInformation("Import finished");
    }

```

## 7) MAUI Page example (typical)

### After (NLog-native)
```

using LogCtxShared;
using Microsoft.Extensions.Logging;

public partial class MainPage
{
private readonly ILogger<MainPage> _logger;

    public MainPage(ILogger<MainPage> logger)
    {
        InitializeComponent();
        _logger = logger;
    }
    
    private void OnClicked(object sender, EventArgs e)
    {
        using (_logger.SetContext(new Props()
            .Add("UiAction", "Clicked")
            .Add("Page", nameof(MainPage))))
        {
            _logger.LogInformation("Button clicked");
        }
    }
    }

```

## 8) “Log object” pattern

### After (NLog-native)
```

using LogCtxShared;
using Microsoft.Extensions.Logging;

var payload = new { OrderId = orderId, Lines = lines.Count };

using (_logger.SetContext(new Props().AddJson("Payload", payload)))
{
_logger.LogInformation("Sending payload");
}

```
