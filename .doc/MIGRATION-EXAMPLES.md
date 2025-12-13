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

## 2) Scoped context ("Set pattern")

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

## 8) "Log object" pattern

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

## 9) Test setup with shared logger factory

### After (NLog-native)
```

using Microsoft.Extensions.Logging;
using NUnit.Framework;
using LogCtxShared;

[SetUpFixture]
public sealed class Logging
{
public static ILoggerFactory Factory { get; private set; } = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        Factory = MauiSetup.CreateLoggerFactory(
            configuration: null,
            seqUrl: "http://localhost:5341",
            apiKey: null,
            nlogConfigFileName: "NLog.config");
    }
    
    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        Factory.Dispose();
        NLog.LogManager.Shutdown();
    }
    }

[TestFixture]
public class MyTests
{
private ILogger<MyTests> _logger = null!;

    [SetUp]
    public void Setup()
    {
        _logger = Logging.Factory.CreateLogger<MyTests>();
    }
    
    [Test]
    public void TestWithContext()
    {
        using (_logger.SetContext(new Props().Add("TestId", 123)))
        {
            _logger.LogInformation("Test action");
        }
    }
    }

```

## 10) NLog.config with SEQ target

### After (NLog-native)
```

<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
      autoReload="true"
      throwConfigExceptions="true">

  <!-- Variables for configuration -->
  <variable name="seqUrl" value="http://localhost:5341"/>
  <variable name="seqApiKey" value=""/>
  <!-- Extensions (if SEQ target not auto-discovered) -->
  <extensions>
    <add assembly="NLog.Targets.Seq"/>
  </extensions>
  <!-- Targets for logging -->
  <targets async="true">
    <!-- Console target for local debugging -->
    <target xsi:type="Console" name="console"
            layout="${longdate}|${level:uppercase=true}|${logger}|${message}${onexception:inner=${newline}${exception:format=toString}}"/>

    <!-- SEQ target for structured logging -->
    <target xsi:type="Seq" name="seq" serverUrl="${var:seqUrl}" apiKey="${var:seqApiKey}">
      <!-- Capture machine/process context -->
      <property name="MachineName" value="${machinename}" />
      <property name="ProcessId" value="${processid}" />
      <property name="ThreadId" value="${threadid}" />
      <!-- BeginScope properties are automatically captured -->
    </target>
  </targets>
  <!-- Logging rules -->
  <rules>
    <!-- Console: Debug and above -->
    <logger name="*" minlevel="Debug" writeTo="console"/>

    <!-- SEQ: Trace and above for full visibility -->
    <logger name="*" minlevel="Trace" writeTo="seq"/>
  </rules>
</nlog>

```

## 11) MauiProgram.cs setup

### After (NLog-native)
```

using LogCtxShared;
using Microsoft.Extensions.Logging;

public static class MauiProgram
{
public static MauiApp CreateMauiApp()
{
var builder = MauiApp.CreateBuilder();
builder
.UseMauiApp<App>()
.ConfigureFonts(fonts =>
{
fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
});

        // Configure NLog with SEQ
        builder.Logging.AddNLogLogging(
            builder.Configuration,
            seqUrl: "http://192.168.1.100:5341", // Use device-accessible IP for physical devices
            apiKey: null,
            nlogConfigFileName: "NLog.config");
    
        return builder.Build();
    }
    }

```

## 12) SEQ query examples

Once migrated, use these queries in SEQ to leverage structured properties:

### Query by property
```

OrderId = 123

```

### Query by operation
```

Operation = 'ProcessOrder'

```

### Query by source trace
```

CTXSTRACE like '%OrderService%'

```

### Query by multiple properties
```

OrderId = 123 and CustomerId = 456

```

### Query by time + property
```

OrderId = 123 and @Timestamp > Now() - 1h

```

### Query for exceptions with context
```

@Exception is not null and UserId is not null

```

## 13) Android-specific logging pattern

### After (NLog-native)
```

using LogCtxShared;
using Microsoft.Extensions.Logging;

public partial class MainActivity : MauiAppCompatActivity
{
private readonly ILogger<MainActivity> _logger;

    public MainActivity(ILogger<MainActivity> logger)
    {
        _logger = logger;
    }
    
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
    
        using (_logger.SetContext(new Props()
            .Add("Activity", nameof(MainActivity))
            .Add("Lifecycle", "OnCreate")))
        {
            _logger.LogInformation("Activity created");
        }
    }
    }

```

## 14) Complex object logging with JSON

### After (NLog-native)
```

using LogCtxShared;
using Microsoft.Extensions.Logging;

var order = new Order
{
Id = orderId,
CustomerId = customerId,
Lines = new[] { new OrderLine { ProductId = 1, Quantity = 2 } }
};

// For querying by scalar properties + preserving full object
using (_logger.SetContext(new Props()
.Add("OrderId", order.Id)
.Add("CustomerId", order.CustomerId)
.AddJson("OrderDetails", order)))
{
_logger.LogInformation("Order processed");
}

// In SEQ:
// - Query by OrderId = 123 (scalar, fast)
// - View OrderDetails property (JSON, full object)

```
