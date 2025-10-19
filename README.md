# LogCtx AI Code Generation Guide

**Version:** 1.0
**Target:** NLog + SEQ structured logging
**Framework:** .NET 8.0, NUnit + Shouldly

***

## Core Architecture

### Key Components

**LogCtx** provides context-aware logging with automatic caller information capture through three main layers:

1. **LogCtxShared** - Core abstractions (`ILogCtxLogger`, `IScopeContext`, `LogCtx`, `Props`)
2. **NLogShared** - NLog adapter implementation (`CtxLogger`, `NLogScopeContext`)
3. **SeriLogShared** - Serilog adapter implementation (alternative backend)

***

## Pattern Library

### 1. Logger Initialization Pattern

```csharp
// ✅ CORRECT: Dispose logger properly
using var log = new CtxLogger();

// ✅ CORRECT: Explicit configuration path
var log = new CtxLogger("Config/LogConfig.xml");

// ❌ WRONG: Missing disposal
var log = new CtxLogger(); // Memory leak risk
log.Info("test");
// No dispose called
```

**Rules:**

- Always use `using` or explicit `Dispose()` call[^1]
- Logger initialization never throws - uses failsafe fallback[^1]
- Default config path: `Config/LogConfig.xml`[^1]

***

### 2. Structured Context Pattern

```csharp
// ✅ CORRECT: Context with typed parameters
log.Ctx.Set(new Props("userId", 123, true));
log.Info("User action completed");

// ✅ CORRECT: Context with named properties
log.Ctx.Set(new Props()
    .Add("OrderId", orderId)
    .Add("Status", status));
log.Debug("Order processed");

// ❌ WRONG: No context set before logging
log.Info("Important message"); // Missing context for SEQ filtering
```

**Rules:**

- Call `Ctx.Set()` before logging to attach scope properties[^1]
- `Props` constructor auto-names params as `P00`, `P01`, `P02`...[^1]
- Use `Add()` for explicit key naming[^1]
- Context persists until next `Set()` call (auto-clears)[^1]

***

### 3. Stack Trace Context Pattern

```csharp
// ✅ CORRECT: Automatic CTX_STRACE injection
log.Ctx.Set(new Props("action", "data"));
log.Info("Event occurred");
// SEQ receives: CTX_STRACE = "FileName::MethodName::LineNumber"

// ✅ CORRECT: Custom source marker
var marker = log.Ctx.Src("checkpoint");
log.Debug($"Reached {marker}");
```

**Rules:**

- `CTX_STRACE` auto-generated on every `Ctx.Set()`[^1]
- Format: `FileName::MethodName::LineNumber\r\n--filtered stack frames`[^1]
- Filters out `System.*`, `NUnit.*`, `NLog.*`, `TechTalk.*` frames[^1]
- Use `Src()` for lightweight file/method/line tokens[^1]

***

### 4. Exception Logging Pattern

```csharp
// ✅ CORRECT: Error with context
try {
    PerformOperation();
} catch (Exception ex) {
    log.Ctx.Set(new Props("operation", "create", "retries", retryCount));
    log.Error(ex, "Operation failed after retries");
}

// ✅ CORRECT: Fatal with structured data
log.Ctx.Set(new Props()
    .AddJson("Config", configObject));
log.Fatal(ex, "Startup configuration invalid");

// ❌ WRONG: No context for exception
catch (Exception ex) {
    log.Error(ex, "Error"); // Missing diagnostic context
}
```

**Rules:**

- Always set context before error/fatal logs[^1]
- Use `AddJson()` for complex objects in context[^1]
- Exception passed as first parameter to `Error()`/`Fatal()`[^1]

***

### 5. Test Assertion Pattern

```csharp
[Test]
public void Debug_Writes_To_MemoryTarget_With_Message()
{
    // Arrange
    var logger = new CtxLogger();
    _memoryTarget.Logs.Clear();

    // Act
    logger.Debug("debug message");
    LogManager.Flush();

    // Assert
    _memoryTarget.Logs.Count.ShouldBe(1);
    _memoryTarget.Logs[^0].ShouldContain("DEBUG|debug message");
    logger.Dispose();
}
```

**Rules:**

- Always clear target before act phase[^1]
- Call `LogManager.Flush()` before assertions[^1]
- Dispose logger in test teardown or explicit call[^1]
- Use `MemoryTarget` for deterministic log capture[^1]

***

## Common Pitfalls

### ❌ PITFALL 1: Context Leakage

```csharp
// WRONG: Context persists across operations
log.Ctx.Set(new Props("user", "admin"));
log.Info("Admin login");
ProcessUserRequest(); // Still has admin context!
log.Info("User request"); // WRONG context attached
```

**Solution:** Always call `Ctx.Set()` before related log statements[^1]

***

### ❌ PITFALL 2: Missing Dispose

```csharp
// WRONG: Logger not disposed
public void ProcessBatch() {
    var log = new CtxLogger();
    log.Info("Batch started");
    // Method exits without dispose - buffer not flushed
}
```

**Solution:** Use `using` pattern or store logger as field with IDisposable[^1]

***

### ❌ PITFALL 3: Null Values in Props

```csharp
// WRONG: Direct null addition
props.Add("OptionalField", null); // Stores "null value" string

// CORRECT: Conditional addition
if (optionalField != null) {
    props.Add("OptionalField", optionalField);
}
```

**Solution:** Props converts null to `"null value"` string - prefer conditional adds[^1]

***

### ❌ PITFALL 4: Configuration Path Errors

```csharp
// WRONG: Relative path from bin folder
var log = new CtxLogger("../../Config/LogConfig.xml");

// CORRECT: Use AppContext.BaseDirectory
var baseDir = AppContext.BaseDirectory;
var configPath = Path.Combine(baseDir, "Config", "LogConfig.xml");
var log = new CtxLogger(configPath);
```

**Solution:** Always use absolute paths or `AppContext.BaseDirectory`[^1]

***

### ❌ PITFALL 5: Shutdown vs Flush

```csharp
// WRONG: Shutdown in Dispose
public void Dispose() {
    LogManager.Shutdown(); // Stops all logging globally!
}

// CORRECT: Flush only
public void Dispose() {
    LogManager.Flush();
}
```

**Solution:** Use `Flush()` not `Shutdown()` in instance disposal[^1]

***

## SEQ Integration Patterns

### Query-Friendly Property Naming

```csharp
// ✅ CORRECT: SEQ filterable properties
log.Ctx.Set(new Props()
    .Add("UserId", userId)
    .Add("OrderId", orderId)
    .Add("Action", "checkout"));
log.Info("Order placed");

// SEQ Query: UserId = '12345' AND Action = 'checkout'
```


### JSON Serialization for Complex Objects

```csharp
// ✅ CORRECT: Structured logging of DTOs
var order = new Order { Id = 1, Total = 99.99m };
log.Ctx.Set(new Props().AddJson("Order", order));
log.Info("Order created");

// SEQ receives: Order = { "Id": 1, "Total": 99.99 }
```


***

## Code Generation Templates

### Template 1: Standard Log Statement

```csharp
log.Ctx.Set(new Props([param1], [param2], ...));
log.[Level]([exception,] "[message]");
```

**Levels:** `Trace`, `Debug`, `Info`, `Warn`, `Error`, `Fatal`[^1]

***

### Template 2: Operation Block

```csharp
try
{
    log.Ctx.Set(new Props([contextParams]));
    log.Info("[operation] started");
    
    [operation code]
    
    log.Info("[operation] completed");
}
catch (Exception ex)
{
    log.Ctx.Set(new Props([errorContext]));
    log.Error(ex, "[operation] failed");
    throw;
}
```


***

### Template 3: Test Method

```csharp
[Test]
public void [MethodName]_[Scenario]_[ExpectedBehavior]()
{
    // Arrange
    var logger = new CtxLogger();
    _memoryTarget.Logs.Clear();

    // Act
    [test action]
    LogManager.Flush();

    // Assert
    _memoryTarget.Logs.Count.ShouldBe([expectedCount]);
    _memoryTarget.Logs[^0].ShouldContain("[expected content]");
    logger.Dispose();
}
```


***

## Configuration Patterns

### Minimal NLog XML

```xml
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd">
  <targets>
    <target xsi:type="Seq" 
            name="seq" 
            serverUrl="http://localhost:5341" 
            apiKey="">
      <property name="CTX_STRACE" layout="${scopeproperty:CTX_STRACE}" />
      <property name="P00" layout="${event-properties:P00}" />
    </target>
  </targets>
  <rules>
    <logger name="*" minlevel="Trace" writeTo="seq" />
  </rules>
</nlog>
```

**Key:** Always include `CTX_STRACE` and `Pxx` properties in SEQ target[^1]

***

## Testing Checklist

When generating test code, ensure:

- [ ] `[SetUp]` resets `LogManager.Configuration`[^1]
- [ ] `[TearDown]` calls `LogManager.Flush()`[^1]
- [ ] MemoryTarget layout includes `CTX_STRACE` and event properties[^1]
- [ ] Tests clear `_memoryTarget.Logs` before act phase[^1]
- [ ] Assertions check log count, level, message, and context[^1]
- [ ] Logger disposed after each test[^1]

***

## Performance Considerations

### Avoid String Interpolation in Log Messages

```csharp
// ❌ WRONG: Eager evaluation
log.Info($"Processing {expensiveCalculation()}"); 

// ✅ CORRECT: Use context properties
log.Ctx.Set(new Props("value", expensiveCalculation()));
log.Info("Processing value");
```


### Batch Context Updates

```csharp
// ❌ WRONG: Multiple Set() calls
log.Ctx.Set(new Props("A", 1));
log.Ctx.Set(new Props("B", 2)); // Clears A!

// ✅ CORRECT: Single Set() with all properties
log.Ctx.Set(new Props()
    .Add("A", 1)
    .Add("B", 2));
```


***

## Summary

When generating LogCtx-based logging code:

1. **Always** use `using var log = new CtxLogger();`
2. **Always** call `Ctx.Set()` before logging
3. **Never** call `LogManager.Shutdown()` in instance methods
4. **Always** use `MemoryTarget` + `Flush()` in tests
5. **Prefer** named properties over positional `Pxx` keys for SEQ queries

This guide ensures consistent, testable, SEQ-friendly logging patterns across the codebase.[^1]
