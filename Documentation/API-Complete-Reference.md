# LogCtx API Complete Reference

**Comprehensive API reference for LogCtx structured logging library - version 0.3.1**

This document provides complete API reference, terminology definitions, and architectural overview for LogCtx.

---

## 📋 **Terminology Reference Table**

### **Core Classes & Components**

| **Class/Component** | **Purpose** | **Namespace** | **Description** |
|---------------------|-------------|---------------|-----------------|
| **FailsafeLogger** | Initializer | `NLogShared` | Robust NLog configuration manager - **ONLY** correct way to initialize LogCtx |
| **LogCtx** | Factory | `LogCtxShared` | Static factory for creating logging contexts with automatic source capture |
| **CtxLogger** | Adapter | `LogCtxShared` | Context-aware logger that wraps ILogger for structured logging |
| **Props** | Properties | `LogCtxShared` | Structured property collection builder for context enrichment |
| **ILogger** | Interface | `NLog` | Standard NLog logger interface used by CtxLogger |

### **Key Methods & Operations**

| **Method/Property** | **Class** | **Usage** | **Description** |
|---------------------|-----------|-----------|-----------------|
| `Initialize(string)` | `FailsafeLogger` | **MANDATORY** | Initialize LogCtx - call once per application |
| `Set()` | `LogCtx` | Context Creation | Create new logging context with automatic source capture |
| `Logger` | `LogCtx` | Static Property | Global logger instance (null until initialization) |
| `AddProperty(key, value)` | Context | Property Builder | Add structured property to current context |
| `Information(msg, ctx)` | `CtxLogger` | Logging | Log information with context |
| `Error(msg, ex, ctx)` | `CtxLogger` | Error Logging | Log error with exception and context |

---

## 🚨 **CRITICAL INITIALIZATION PATTERNS**

### **❌ WRONG PATTERNS - Will Crash Your Application:**

```csharp
// DON'T USE THESE - THEY DON'T EXIST OR ARE WRONG!
LogCtx.InitLogCtx();                    // ❌ Method doesn't exist
LogCtx.Initialize();                    // ❌ Method doesn't exist  
NLog.LogManager.Setup();                // ❌ Bypasses LogCtx initialization
using LogCtxShared; // Only one namespace // ❌ Missing required namespace
```

### **✅ CORRECT PATTERNS - Copy These Exactly:**

```csharp
// ✅ REQUIRED USING STATEMENTS - BOTH NEEDED!
using NLogShared;   // Required for FailsafeLogger
using LogCtxShared; // Required for LogCtx classes

// ✅ MANDATORY INITIALIZATION - ONLY CORRECT WAY!
FailsafeLogger.Initialize("NLog.config");

// ✅ CORRECT CONTEXT USAGE
using var ctx = LogCtx.Set(); // Automatic disposal
ctx.AddProperty("Key", "Value");
LogCtx.Logger.Information("Message", ctx);
```

---

## 📦 **Package Version Reference**

### **Current Supported Versions (October 2025)**

| **Package** | **Version** | **Purpose** | **Required** |
|-------------|-------------|-------------|--------------|
| **NLog** | **6.0.4** | Core logging framework | ✅ Mandatory |
| **NLog.Extensions.Logging** | **6.0.4** | .NET logging integration | ✅ Recommended |
| **NLog.Targets.Seq** | **6.0.0** | SEQ structured logging target | 🔶 Optional |
| **NLog.Targets.File** | **6.0.4** | File logging target | ✅ Recommended |
| **NLog.Schema** | **6.0.4** | Intellisense support | 🔶 Optional |

### **Framework Compatibility**

| **Framework** | **Minimum Version** | **Recommended** | **Status** |
|---------------|---------------------|-----------------|------------|
| **.NET** | 8.0 | 8.0+ | ✅ Supported |
| **.NET Core** | 6.0 | 8.0+ | ✅ Supported |  
| **.NET Framework** | 4.8 | - | ⚠️ Legacy Support |
| **C#** | 10.0 | 12.0+ | ✅ Supported |

### **Testing Framework Versions**

| **Framework** | **Version** | **Integration** | **Status** |
|---------------|-------------|-----------------|------------|
| **NUnit** | **4.2.2+** | Full LogCtx integration | ✅ Recommended |
| **xUnit** | **2.6.1+** | Basic LogCtx support | 🔶 Supported |
| **MSTest** | **3.1.1+** | Basic LogCtx support | 🔶 Supported |
| **Shouldly** | **4.2.1+** | Assertion library | ✅ Recommended |

---

## 🏗️ **Architecture Overview**

### **Component Relationships**

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│  Application    │───▶│  FailsafeLogger │───▶│     NLog        │
│                 │    │   (Initializer) │    │   (Framework)   │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                        │                        │
         │                        ▼                        ▼
         │              ┌─────────────────┐    ┌─────────────────┐
         └─────────────▶│     LogCtx      │───▶│   CtxLogger     │
                        │   (Factory)     │    │   (Adapter)     │
                        └─────────────────┘    └─────────────────┘
                                 │                        │
                                 ▼                        ▼
                        ┌─────────────────┐    ┌─────────────────┐
                        │     Context     │───▶│     Props       │
                        │   (Instance)    │    │ (Properties)    │
                        └─────────────────┘    └─────────────────┘
```

### **Initialization Flow**

```
Application Startup
        │
        ▼
FailsafeLogger.Initialize("NLog.config")
        │
        ▼ 
NLog Configuration Loaded
        │
        ▼
LogCtx.Logger Available (CtxLogger instance)
        │
        ▼
Application Can Create Contexts
        │
        ▼
using var ctx = LogCtx.Set()
        │
        ▼
Context Captures Source Location
        │
        ▼
ctx.AddProperty() - Build Structured Data
        │
        ▼
LogCtx.Logger.Information(message, ctx)
        │
        ▼
Structured Log Entry Created
        │
        ▼
Context Disposed (using statement)
```

---

## 🔧 **Standard Code Templates**

### **Template: Application Initialization**

```csharp
// ✅ STANDARD APPLICATION TEMPLATE
using NLogShared;   // Required for FailsafeLogger
using LogCtxShared; // Required for LogCtx classes
using NLog;

namespace {YOUR_NAMESPACE}
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            // ⚠️ MANDATORY - Initialize LogCtx first!
            FailsafeLogger.Initialize("NLog.config");
            
            // Application startup context
            using var startupCtx = LogCtx.Set();
            startupCtx.AddProperty("ApplicationName", "{APP_NAME}");
            startupCtx.AddProperty("Version", "{VERSION}");
            LogCtx.Logger.Information("Application starting", startupCtx);
            
            try
            {
                // Your application code here
                {APPLICATION_CODE}
            }
            catch (Exception ex)
            {
                using var errorCtx = LogCtx.Set();
                errorCtx.AddProperty("ApplicationName", "{APP_NAME}");
                errorCtx.AddProperty("ErrorType", ex.GetType().Name);
                LogCtx.Logger.Fatal("Application startup failed", ex, errorCtx);
                throw;
            }
        }
    }
}
```

### **Template: Service Method**

```csharp
// ✅ STANDARD SERVICE METHOD TEMPLATE
using NLogShared;   // Required for FailsafeLogger
using LogCtxShared; // Required for LogCtx classes

public async Task<{RETURN_TYPE}> {METHOD_NAME}Async({PARAMETERS})
{
    // Create method context
    using var ctx = LogCtx.Set(); // ✅ Automatic source capture
    ctx.AddProperty("ServiceName", nameof({SERVICE_CLASS}));
    ctx.AddProperty("Operation", nameof({METHOD_NAME}));
    // Add parameter properties here
    LogCtx.Logger.Information("Operation started", ctx);

    try
    {
        // Your business logic here
        {BUSINESS_LOGIC}
        
        // Success logging
        LogCtx.Logger.Information("Operation completed successfully", ctx);
        return {RESULT};
    }
    catch (Exception ex)
    {
        // Error context with enrichment  
        ctx.AddProperty("ErrorType", ex.GetType().Name);
        LogCtx.Logger.Error("Operation failed", ex, ctx);
        throw;
    }
}
```

### **Template: Unit Test Setup**

```csharp
// ✅ STANDARD UNIT TEST TEMPLATE
using NUnit.Framework;
using Shouldly;
using NLogShared;   // Required for FailsafeLogger
using LogCtxShared; // Required for LogCtx classes

[TestFixture]
public class {TEST_CLASS}Tests
{
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        // ⚠️ MANDATORY - Initialize once per test fixture
        FailsafeLogger.Initialize("NLog.config");
    }

    [Test]
    public void {TEST_METHOD}()
    {
        // Test context
        using var testCtx = LogCtx.Set();
        testCtx.AddProperty("TestClass", nameof({TEST_CLASS}Tests));
        testCtx.AddProperty("TestMethod", nameof({TEST_METHOD}));
        LogCtx.Logger.Information("Test execution started", testCtx);

        try
        {
            // Arrange, Act, Assert
            {TEST_CODE}
            
            LogCtx.Logger.Information("Test execution completed", testCtx);
        }
        catch (Exception ex)
        {
            using var errorCtx = LogCtx.Set();
            errorCtx.AddProperty("TestClass", nameof({TEST_CLASS}Tests));
            errorCtx.AddProperty("TestMethod", nameof({TEST_METHOD}));
            errorCtx.AddProperty("ErrorType", ex.GetType().Name);
            LogCtx.Logger.Error("Test execution failed", ex, errorCtx);
            throw;
        }
    }
}
```

---

## 📋 **Property Naming Standards**

### **Required Properties (Use These Names)**

| **Property Name** | **Type** | **Usage** | **Example** |
|-------------------|----------|-----------|-------------|
| `ServiceName` | `string` | Service/class identifier | `"UserService"` |
| `Operation` | `string` | Method/operation name | `"GetUserById"` |
| `RequestId` | `string` | Request correlation | `Guid.NewGuid().ToString()` |
| `ErrorType` | `string` | Exception type name | `ex.GetType().Name` |
| `UserId` | `int/string` | User identifier | `123` or `"user123"` |
| `Duration` | `long` | Operation time in ms | `stopwatch.ElapsedMilliseconds` |

### **Optional Properties (Contextual)**

| **Property Name** | **Type** | **Usage** | **Example** |
|-------------------|----------|-----------|-------------|
| `FilePath` | `string` | File operations | `"/path/to/file.txt"` |
| `FileSize` | `long` | File metrics | `fileInfo.Length` |
| `RecordCount` | `int` | Data volume | `results.Count` |
| `BatchSize` | `int` | Batch processing | `100` |
| `StatusCode` | `int` | HTTP operations | `200` |
| `TableName` | `string` | Database operations | `"Users"` |

---

## 🔍 **Namespace Usage Guide**

### **Why Both Namespaces Are Required**

| **Namespace** | **Contains** | **Used For** |
|---------------|--------------|--------------|
| **`NLogShared`** | `FailsafeLogger` | ✅ **MANDATORY** - Initialization only |
| **`LogCtxShared`** | `LogCtx`, `CtxLogger`, `Props` | ✅ **MANDATORY** - All logging operations |

### **Import Statement Rules**

```csharp
// ✅ ALWAYS INCLUDE BOTH - NO EXCEPTIONS!
using NLogShared;   // Required for FailsafeLogger.Initialize()
using LogCtxShared; // Required for LogCtx.Set(), LogCtx.Logger, etc.

// Additional imports as needed
using NLog;         // If you need direct NLog access  
using System;       // Standard system imports
using Microsoft.Extensions.Logging; // If using .NET logging
```

---

## ⚠️ **Common Mistakes & Solutions**

### **Mistake 1: Wrong Initialization Method**
```csharp
// ❌ WRONG
LogCtx.InitLogCtx(); // Method doesn't exist!

// ✅ CORRECT  
FailsafeLogger.Initialize("NLog.config");
```

### **Mistake 2: Missing Using Statements**
```csharp
// ❌ WRONG - Missing required namespace
using LogCtxShared;
FailsafeLogger.Initialize("NLog.config"); // Compilation error!

// ✅ CORRECT - Both namespaces included
using NLogShared;   // For FailsafeLogger
using LogCtxShared; // For LogCtx classes
FailsafeLogger.Initialize("NLog.config"); // Works!
```

### **Mistake 3: Context Not Disposed**
```csharp
// ❌ WRONG - Memory leak potential
var ctx = LogCtx.Set();
LogCtx.Logger.Information("Test", ctx);
// Context never disposed!

// ✅ CORRECT - Automatic disposal
using var ctx = LogCtx.Set();
LogCtx.Logger.Information("Test", ctx);
// Context automatically disposed
```

### **Mistake 4: Multiple Initialization**
```csharp
// ❌ WRONG - Initializing multiple times
FailsafeLogger.Initialize("NLog.config");
// ... later in code ...
FailsafeLogger.Initialize("NLog.config"); // Unnecessary!

// ✅ CORRECT - Initialize once per application
// In Main() method or OneTimeSetUp():
FailsafeLogger.Initialize("NLog.config"); // Only once!
```

---

## 📊 **Performance Guidelines**

### **Context Creation Costs**

| **Operation** | **Cost** | **Recommendation** |
|---------------|----------|-------------------|
| `LogCtx.Set()` | Low | Use freely for operations |
| Context disposal | Very Low | Always use `using` statements |
| Property addition | Minimal | Add meaningful properties |
| Logging call | Low-Medium | Don't log in tight loops |

### **Best Practices**

1. ✅ **Create contexts** for logical operations
2. ✅ **Reuse contexts** within operation scope  
3. ✅ **Add meaningful properties** for SEQ queries
4. ✅ **Use batch logging** for high-frequency operations
5. ❌ **Don't create contexts** in tight loops without batching

---

## 🔗 **Reference Links**

- **[README-CORRECTED.md]** - Quick start and overview
- **[Step-0-Integration-Guide-CORRECTED.md]** - Complete integration guide
- **[Usage-Patterns-Examples-CORRECTED.md]** - Real-world usage examples
- **[AI-Code-Generation-Guide-CORRECTED.md]** - AI-assisted development patterns

---

**Version:** 0.3.1  
**Last Updated:** October 2025  
**API Stability:** Stable - Breaking changes will increment major version