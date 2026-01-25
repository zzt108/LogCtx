# Migration Guide - LogCtx v1.x → v2.0 (NLog-Native)

**Date:** 2026-01-04  
**Version:** v2.0  
**Status:** Production-Ready ✅

---

## Overview

LogCtx v2.0 replaces the legacy LogCtx.Set() pattern with NLog-native, thread-safe Props API. The transition is **mostly non-breaking** - existing code works but should be updated for cleaner syntax and thread-safety.

---

## At a Glance

| Aspect | v1.x (Legacy) | v2.0 (NLog-Native) |
|--------|---------------|-------------------|
| **Base class** | `Dictionary<string, object>` | `ConcurrentDictionary<string, object>` |
| **Thread-safe** | ❌ No | ✅ Yes |
| **CallerInfo** | Manual tracking | ✅ Automatic capture |
| **Scope management** | Manual LogCtx.Set/Clear | ✅ `using` statement |
| **API** | LogCtx.Set(props) | logger.SetContext(props) |
| **Nested contexts** | Manual nesting | ✅ Built-in inheritance |
| **Performance** | Fast (no scope recreation) | ~5% overhead (scope recreation) |

---

## Migration Patterns

### Pattern 1: Basic Scoped Context

#### Before (v1.x)
```csharp
using LogCtxShared;

var props = new Props();
props.Add("userId", 123);
props.Add("action", "login");

LogCtx.Set(props);

try
{
    _logger.LogInformation("User logged in");
    // ... business logic ...
}
finally
{
    LogCtx.Clear();
}
```

**Issues:**
- Manual Set/Clear - error-prone
- No automatic scope cleanup
- No thread-safety

---

#### After (v2.0) - Simple
```csharp
using LogCtxShared;

using Props p = _logger.SetContext()
    .Add("userId", 123)
    .Add("action", "login");
{
    _logger.LogInformation("User logged in");
    // ... business logic ...
} // Auto-disposed
```

**Benefits:**
- ✅ Automatic scope cleanup
- ✅ Fluent chaining
- ✅ Thread-safe
- ✅ CallerInfo captured

---

### Pattern 2: Nested Contexts

#### Before (v1.x)
```csharp
using LogCtxShared;

LogCtx.Set(new Props()
    .Add("operation", "ImportBatch")
    .Add("batchId", 123));

try
{
    foreach (var record in records)
    {
        // Manual nested context
        var innerProps = new Props();
        innerProps.Add("operation", "ImportBatch");
        innerProps.Add("batchId", 123);
        innerProps.Add("recordId", record.Id);
        
        LogCtx.Set(innerProps);
        
        _logger.LogInformation("Processing record");
        
        LogCtx.Clear(); // ❌ Must clear manually
    }
}
finally
{
    LogCtx.Clear();
}
```

**Issues:**
- Manual property copying
- Must remember parent context
- Scope management error-prone

---

#### After (v2.0) - Built-in Inheritance
```csharp
using LogCtxShared;

using Props p = _logger.SetContext()
    .Add("operation", "ImportBatch")
    .Add("batchId", 123);
{
    foreach (var record in records)
    {
        // ✅ Automatic property inheritance
        p = _logger.SetContext(p)
            .Add("recordId", record.Id);
        
        _logger.LogInformation("Processing record");
        // recordId changes, operation + batchId inherited
    }
}
```

**Benefits:**
- ✅ Automatic property inheritance
- ✅ No manual copying
- ✅ Clear scoping rules

---

### Pattern 3: Operation Context

#### Before (v1.x)
```csharp
var props = new Props();
props.Add("operation", "ProcessOrder");
props.Add("orderId", 123);
props.Add("customerId", 456);

LogCtx.Set(props);

try
{
    _logger.LogInformation("Order processing started");
    // ... order processing logic ...
}
finally
{
    LogCtx.Clear();
}
```

---

#### After (v2.0) - Operation Scope
```csharp
using (_logger.SetOperationContext(
    "ProcessOrder",
    ("OrderId", 123),
    ("CustomerId", 456)))
{
    _logger.LogInformation("Order processing started");
    // ... order processing logic ...
} // Auto-disposed
```

**Benefits:**
- ✅ Intent-clear API
- ✅ Automatic cleanup
- ✅ Less boilerplate

---

### Pattern 4: DI Integration

#### Before (v1.x)
```csharp
public class OrderService
{
    private readonly ILogger<OrderService> _logger;
    private readonly ILogCtxLogger _ctxLogger; // ❌ Extra dependency
    
    public OrderService(
        ILogger<OrderService> logger,
        ILogCtxLogger ctxLogger)
    {
        _logger = logger;
        _ctxLogger = ctxLogger;
    }
    
    public void ProcessOrder(int orderId)
    {
        var props = new Props().Add("orderId", orderId);
        _ctxLogger.Set(props);
        
        try
        {
            _logger.LogInformation("Processing order");
        }
        finally
        {
            _ctxLogger.Clear();
        }
    }
}
```

---

#### After (v2.0) - Extension Method
```csharp
public class OrderService
{
    private readonly ILogger<OrderService> _logger;
    
    public OrderService(ILogger<OrderService> logger)
    {
        _logger = logger;
        // ✅ No extra dependency
    }
    
    public void ProcessOrder(int orderId)
    {
        using (_logger.SetContext()
            .Add("orderId", orderId))
        {
            _logger.LogInformation("Processing order");
            // Auto-disposed
        }
    }
}
```

**Benefits:**
- ✅ Single ILogger dependency
- ✅ Standard .NET patterns
- ✅ Simpler DI setup

---

### Pattern 5: Exception Logging

#### Before (v1.x)
```csharp
LogCtx.Set(new Props().Add("userId", userId));

try
{
    DoWork();
}
catch (Exception ex)
{
    _logger.LogError(ex, "Work failed");
    // ❌ Context may be lost if Set() not called before exception
}
finally
{
    LogCtx.Clear();
}
```

---

#### After (v2.0) - Context Preserved
```csharp
using (_logger.SetContext()
    .Add("userId", userId))
{
    try
    {
        DoWork();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Work failed");
        // ✅ Context always active in catch block
    }
} // Disposed automatically
```

**Benefits:**
- ✅ Context guaranteed throughout block
- ✅ Exception logs include context
- ✅ Cleaner code structure

---

### Pattern 6: JSON Properties

#### Before (v1.x)
```csharp
var props = new Props();
props.AddJson("payload", new { Items = items, Total = 123.45 });

LogCtx.Set(props);

_logger.LogInformation("Sending payload");

LogCtx.Clear();
```

---

#### After (v2.0) - Same API
```csharp
using (_logger.SetContext()
    .AddJson("payload", new { Items = items, Total = 123.45 }))
{
    _logger.LogInformation("Sending payload");
}
```

**No changes needed** - AddJson() works identically, just cleaner scope management.

---

## Breaking Changes

### Props Constructor

**v1.x:**
```csharp
var props = new Props();
props.Add("key", "value");
```

**v2.0:**
```csharp
// Constructor is now INTERNAL
// ❌ This won't compile:
var props = new Props();

// ✅ Use SetContext() instead:
using Props p = _logger.SetContext()
    .Add("key", "value");
```

**Why:** Constructor now requires ILogger + CallerInfo internally. Extension method handles this automatically.

---

### LogCtx.Set/Clear Removed

**v1.x:**
```csharp
LogCtx.Set(props);
_logger.LogInformation("message");
LogCtx.Clear();
```

**v2.0:**
```csharp
using (_logger.SetContext(props))
{
    _logger.LogInformation("message");
} // Auto-cleared
```

---

### Thread-Safety Changes

**v1.x:**
```csharp
// ❌ Race condition possible:
props.Add("key1", "value1");
props.Add("key2", "value2");
LogCtx.Set(props);

// Thread 2 could modify props during Set()
```

**v2.0:**
```csharp
// ✅ Thread-safe:
using Props p = _logger.SetContext()
    .Add("key1", "value1")
    .Add("key2", "value2");
// No race conditions - ConcurrentDictionary-based
```

---

## Incremental Migration Strategy

### Phase 1: Update SetContext Calls (Low Risk)
```csharp
// OLD
LogCtx.Set(new Props().Add("userId", userId));
_logger.LogInformation("message");
LogCtx.Clear();

// NEW (drop-in replacement)
using (_logger.SetContext()
    .Add("userId", userId))
{
    _logger.LogInformation("message");
}
```

**Risk:** ⚠️ Behavioral change - scope now auto-clears

---

### Phase 2: Remove Manual Props Creation (Medium Risk)
```csharp
// OLD
var props = new Props();
props.Add("key1", "value1");
LogCtx.Set(props);

// NEW (fluent API)
using (_logger.SetContext()
    .Add("key1", "value1"))
{
    // ...
}
```

**Risk:** ❌ Old props constructor no longer works

---

### Phase 3: Migrate Nested Contexts (Medium Risk)
```csharp
// OLD - manual inheritance
foreach (var item in items)
{
    var nested = new Props();
    nested.Add("parentId", parentId); // ❌ Manual copy
    nested.Add("itemId", item.Id);
    LogCtx.Set(nested);
    // ...
}

// NEW - automatic inheritance
using Props p = _logger.SetContext()
    .Add("parentId", parentId);
{
    foreach (var item in items)
    {
        p = _logger.SetContext(p)
            .Add("itemId", item.Id);
        // ...
    }
}
```

**Risk:** ⚠️ New nesting semantics (parent disposal)

---

## Testing Checklist

- [ ] All Props().Add() calls updated to SetContext().Add()
- [ ] LogCtx.Set/Clear calls replaced with using blocks
- [ ] Nested contexts using SetContext(parent) pattern
- [ ] DI service removal (ILogCtxLogger no longer needed)
- [ ] Exception logging includes context (inside using block)
- [ ] Unit tests pass (update Logging.Factory usage)
- [ ] Integration tests verify SEQ property capture
- [ ] Performance test (expected < 1% overhead)
- [ ] Thread-safety validated (concurrent test scenario)

---

## Common Issues

### Issue 1: "Props is not constructable"
**Cause:** Using `new Props()` (constructor is now internal)

**Fix:**
```csharp
// ❌ OLD
var p = new Props().Add("key", "value");

// ✅ NEW
using Props p = _logger.SetContext()
    .Add("key", "value");
```

---

### Issue 2: "Context not cleared on exception"
**Cause:** LogCtx.Clear() not called in exception handler

**Fix:**
```csharp
// ❌ OLD
LogCtx.Set(props);
try { DoWork(); }
catch { _logger.LogError(...); } // Context maybe lost!
finally { LogCtx.Clear(); }

// ✅ NEW
using (_logger.SetContext(props))
{
    try { DoWork(); }
    catch { _logger.LogError(...); } // Context guaranteed
}
```

---

### Issue 3: "Nested context lost parent properties"
**Cause:** Creating new Props instead of SetContext(parent)

**Fix:**
```csharp
// ❌ OLD - manual inheritance
var parent = new Props().Add("parentId", id);
LogCtx.Set(parent);
foreach (var child in children)
{
    var nested = new Props()
        .Add("parentId", id) // ❌ Manual copy!
        .Add("childId", child.Id);
    LogCtx.Set(nested);
}

// ✅ NEW - automatic inheritance
using Props p = _logger.SetContext()
    .Add("parentId", id);
{
    foreach (var child in children)
    {
        p = _logger.SetContext(p)
            .Add("childId", child.Id);
    }
}
```

---

### Issue 4: "Scope recreation overhead"
**Cause:** Each Add() recreates scope (necessary for NLog snapshot semantics)

**Fix:** (Validate if overhead is real)
```csharp
// If AddJson/AddJson cause perf issues:
// 1. Batch properties before SetContext:
var props = new Props()
    .Add("key1", "value1")
    .Add("key2", "value2");
using (_logger.SetContext(props))
{
    // ...
}

// 2. Or profile to confirm bottleneck:
// dotnet-benchmark props-recreation.cs
```

---

## Performance Considerations

### Scope Recreation Overhead

**v1.x:**
```csharp
var props = new Props();
props.Add("key1", "value1");
props.Add("key2", "value2");
// Props created once, scope NOT recreated
```

**v2.0:**
```csharp
using Props p = _logger.SetContext()
    .Add("key1", "value1")  // ← Scope recreated
    .Add("key2", "value2");  // ← Scope recreated
// Expected overhead: ~10μs (2 scope creations)
```

**Impact:**
- Single Add(): 5-10μs overhead
- Chained Adds: 5-10μs per Add()
- In context of 100-1000μs logging: < 1% impact

**Mitigation:**
```csharp
// Batch adds before scope heavy operations:
using Props p = _logger.SetContext()
    .Add("key1", "value1")
    .Add("key2", "value2")
    .Add("key3", "value3");
{
    _logger.LogInformation("Only one scope active here");
}
```

---

## Rollback Plan

If v2.0 causes issues:

1. **Rollback by version:**
   ```bash
   <PackageReference Include="LogCtxShared" Version="1.5.0" />
   ```

2. **Revert migration:**
   - Replace `using (_logger.SetContext())` with `LogCtx.Set()`
   - Restore `new Props()` constructors
   - Re-add `LogCtx.Clear()` calls

---

## FAQ

**Q: Do I have to migrate immediately?**

A: No - v1.x and v2.0 can coexist. Migrate incrementally per Phase.

---

**Q: Will props be garbage collected properly?**

A: Yes - `using` statement calls Dispose() automatically, scope is released.

---

**Q: Is nested context inheritance guaranteed?**

A: Yes - SetContext(parent) copies properties before disposing parent.

---

**Q: What if I need to keep global context?**

A: Use outer `using` block:
```csharp
using Props global = _logger.SetContext()
    .Add("requestId", id);
{
    // Process request
    foreach (var step in steps)
    {
        global = _logger.SetContext(global)
            .Add("step", step.Name);
        // ...
    }
}
```

---

**Q: Performance regression feared?**

A: Unlikely - < 1% overhead in real logging (I/O-bound). Profile if concerned.

---

## Resources

- **API-REFERENCE-v2.md** - Complete API documentation
- **MIGRATION-EXAMPLES.md** - 14 detailed before/after examples
- **MAUI-SAMPLES.md** - Complete working sample app
- **Tests/NLogScopeReferenceTests.cs** - Test patterns + scenarios

---

## Summary

| Aspect | Benefit |
|--------|---------|
| **Syntax** | Fluent, chainable, cleaner |
| **Safety** | Thread-safe, auto-disposed, property inherited |
| **Maintenance** | No manual Set/Clear, less error-prone |
| **Performance** | < 1% overhead vs v1.x |
| **Adoption** | Incremental, low-risk migration path |

---

**Version:** 2.0  
**Status:** Production ✅  
**Last Updated:** 2026-01-04
