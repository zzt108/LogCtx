# LogCtx AI Code Generation Guide

**AI-assisted development patterns for LogCtx structured logging - corrected for version 0.3.1**

This guide provides AI-friendly patterns and examples for generating LogCtx-based logging code that follows best practices and avoids common pitfalls.

---

## 🚨 **CRITICAL: Correct Initialization Pattern**

### **❌ WRONG - This Will Crash Your Application:**
```csharp
// DON'T USE THIS - LogCtx.InitLogCtx() DOESN'T EXIST!
LogCtx.InitLogCtx(); // ❌ This method does not exist and will cause compilation errors
```

### **✅ CORRECT - Use This Pattern:**
```csharp
using NLogShared;   // Required for FailsafeLogger
using LogCtxShared; // Required for LogCtx classes
using NLog;

// ⚠️ MANDATORY INITIALIZATION - This is the ONLY correct way!
FailsafeLogger.Initialize("NLog.config");
```

---

## 🤖 **AI Code Generation Templates**

### **Template 1: Application Startup**

```csharp
// ✅ AI TEMPLATE - Application Main Method
using NLogShared;   // Required for FailsafeLogger
using LogCtxShared; // Required for LogCtx classes
using NLog;

namespace {APPLICATION_NAME}
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            // ⚠️ MANDATORY - Initialize logging FIRST!
            FailsafeLogger.Initialize("NLog.config");
            
            // Application startup context
            using var startupCtx = LogCtx.Set();
            startupCtx.AddProperty("ApplicationName", "{APPLICATION_NAME}");
            startupCtx.AddProperty("Version", "{VERSION}");
            LogCtx.Logger.Information("Application starting", startupCtx);
            
            try
            {
                // Your application initialization code here
                {APPLICATION_INIT_CODE}
            }
            catch (Exception ex)
            {
                using var errorCtx = LogCtx.Set();
                errorCtx.AddProperty("ApplicationName", "{APPLICATION_NAME}");
                errorCtx.AddProperty("ErrorType", ex.GetType().Name);
                LogCtx.Logger.Fatal("Application startup failed", ex, errorCtx);
                throw;
            }
        }
    }
}
```

### **Template 2: Service Method Logging**

```csharp
// ✅ AI TEMPLATE - Service Method with Logging
using NLogShared;   // Required for FailsafeLogger
using LogCtxShared; // Required for LogCtx classes

public class {SERVICE_NAME}Service
{
    public async Task<{RETURN_TYPE}> {METHOD_NAME}Async({PARAMETERS})
    {
        // Step 1: Create method context with automatic source location
        using var ctx = LogCtx.Set(); // ✅ Captures file/line automatically
        ctx.AddProperty("ServiceName", "{SERVICE_NAME}");
        ctx.AddProperty("Operation", "{METHOD_NAME}");
        {PARAMETER_LOGGING} // Add method parameters as properties
        LogCtx.Logger.Information("Operation started", ctx);

        try
        {
            // Step 2: Business logic implementation
            {BUSINESS_LOGIC}
            
            // Step 3: Success logging
            using var successCtx = LogCtx.Set();
            successCtx.AddProperty("ServiceName", "{SERVICE_NAME}");
            successCtx.AddProperty("Operation", "{METHOD_NAME}");
            {RESULT_LOGGING} // Add result properties
            LogCtx.Logger.Information("Operation completed successfully", successCtx);
            
            return {RESULT};
        }
        catch (Exception ex)
        {
            // Step 4: Error context with enrichment
            using var errorCtx = LogCtx.Set();
            errorCtx.AddProperty("ServiceName", "{SERVICE_NAME}");
            errorCtx.AddProperty("Operation", "{METHOD_NAME}");
            errorCtx.AddProperty("ErrorType", ex.GetType().Name);
            {PARAMETER_LOGGING} // Re-add parameters for error context
            LogCtx.Logger.Error("Operation failed", ex, errorCtx);
            throw;
        }
    }
}
```

### **Template 3: Unit Test Setup**

```csharp
// ✅ AI TEMPLATE - Unit Test Class
using NUnit.Framework;
using Shouldly;
using NLogShared;   // Required for FailsafeLogger
using LogCtxShared; // Required for LogCtx classes

[TestFixture]
public class {TEST_CLASS_NAME}
{
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        // ⚠️ MANDATORY INITIALIZATION - Initialize once per test fixture
        FailsafeLogger.Initialize("NLog.config");
    }

    [Test]
    public void {TEST_METHOD_NAME}()
    {
        // Arrange
        using var testCtx = LogCtx.Set();
        testCtx.AddProperty("TestClass", nameof({TEST_CLASS_NAME}));
        testCtx.AddProperty("TestMethod", nameof({TEST_METHOD_NAME}));
        testCtx.AddProperty("TestCategory", "{TEST_CATEGORY}");
        LogCtx.Logger.Information("Test execution started", testCtx);

        try
        {
            // Act
            {TEST_ACTIONS}

            // Assert
            {ASSERTIONS}
            
            LogCtx.Logger.Information("Test execution completed successfully", testCtx);
        }
        catch (Exception ex)
        {
            using var errorCtx = LogCtx.Set();
            errorCtx.AddProperty("TestClass", nameof({TEST_CLASS_NAME}));
            errorCtx.AddProperty("TestMethod", nameof({TEST_METHOD_NAME}));
            errorCtx.AddProperty("ErrorType", ex.GetType().Name);
            LogCtx.Logger.Error("Test execution failed", ex, errorCtx);
            throw;
        }
    }
}
```

### **Template 4: File Processing with Context**

```csharp
// ✅ AI TEMPLATE - File Processing with Rich Context
using NLogShared;   // Required for FailsafeLogger
using LogCtxShared; // Required for LogCtx classes

public class {PROCESSOR_NAME}
{
    public async Task<bool> ProcessFileAsync(string filePath)
    {
        // Step 1: File operation context
        using var ctx = LogCtx.Set(); // ✅ Captures file/line automatically
        ctx.AddProperty("FilePath", filePath);
        ctx.AddProperty("Operation", "ProcessFile");
        LogCtx.Logger.Information("File processing started", ctx);

        try
        {
            // Step 2: File validation
            if (!File.Exists(filePath))
            {
                using var notFoundCtx = LogCtx.Set();
                notFoundCtx.AddProperty("FilePath", filePath);
                notFoundCtx.AddProperty("Operation", "ProcessFile");
                LogCtx.Logger.Warning("File not found", notFoundCtx);
                return false;
            }

            // Step 3: File loading with metrics
            var content = await File.ReadAllTextAsync(filePath);
            using var loadedCtx = LogCtx.Set();
            loadedCtx.AddProperty("FilePath", filePath);
            loadedCtx.AddProperty("FileSize", content.Length);
            loadedCtx.AddProperty("Operation", "ProcessFile");
            LogCtx.Logger.Information("File loaded successfully", loadedCtx);

            // Step 4: Processing logic
            {PROCESSING_LOGIC}

            // Step 5: Success metrics
            using var successCtx = LogCtx.Set();
            successCtx.AddProperty("FilePath", filePath);
            successCtx.AddProperty("Operation", "ProcessFile");
            successCtx.AddProperty("ProcessingResult", "Success");
            LogCtx.Logger.Information("File processing completed", successCtx);

            return true;
        }
        catch (Exception ex)
        {
            // Step 6: Comprehensive error context
            using var errorCtx = LogCtx.Set();
            errorCtx.AddProperty("FilePath", filePath);
            errorCtx.AddProperty("Operation", "ProcessFile");
            errorCtx.AddProperty("ErrorType", ex.GetType().Name);
            errorCtx.AddProperty("ProcessingResult", "Failed");
            LogCtx.Logger.Error("File processing failed", ex, errorCtx);
            throw;
        }
    }
}
```

---

## 🎯 **AI Generation Rules**

### **MANDATORY Requirements:**
1. ✅ **Always include both using statements:**
   ```csharp
   using NLogShared;   // Required for FailsafeLogger
   using LogCtxShared; // Required for LogCtx classes
   ```

2. ✅ **Always use correct initialization:**
   ```csharp
   FailsafeLogger.Initialize("NLog.config");
   ```

3. ✅ **Always use `using` statements with LogCtx.Set():**
   ```csharp
   using var ctx = LogCtx.Set();
   ```

4. ✅ **Always add meaningful properties:**
   ```csharp
   ctx.AddProperty("Operation", "MethodName");
   ctx.AddProperty("ComponentName", "ServiceName");
   ```

### **FORBIDDEN Patterns:**
1. ❌ **Never use `LogCtx.InitLogCtx()`** - This method doesn't exist!
2. ❌ **Never omit using statements** - Missing imports will cause null reference exceptions
3. ❌ **Never forget disposal** - Always use `using var ctx = LogCtx.Set();`
4. ❌ **Never initialize multiple times** - One initialization per application/test fixture

---

## 🔍 **Property Naming Conventions**

### **Standard Properties:**
```csharp
ctx.AddProperty("ServiceName", "UserService");      // Service/component identifier
ctx.AddProperty("Operation", "CreateUser");         // Method/operation name
ctx.AddProperty("UserId", userId);                 // Business entity ID
ctx.AddProperty("RequestId", requestId);           // Request correlation ID
ctx.AddProperty("ErrorType", ex.GetType().Name);   // Exception type
ctx.AddProperty("Duration", stopwatch.ElapsedMs);  // Performance metrics
ctx.AddProperty("FileSize", content.Length);       // Resource metrics
ctx.AddProperty("RecordCount", records.Count);     // Data volume metrics
```

### **Context-Specific Properties:**
```csharp
// File operations
ctx.AddProperty("FilePath", filePath);
ctx.AddProperty("FileSize", fileInfo.Length);
ctx.AddProperty("FileExtension", Path.GetExtension(filePath));

// Database operations
ctx.AddProperty("TableName", "Users");
ctx.AddProperty("QueryType", "SELECT");
ctx.AddProperty("RowCount", resultSet.Count);

// HTTP operations
ctx.AddProperty("HttpMethod", "POST");
ctx.AddProperty("Endpoint", "/api/users");
ctx.AddProperty("StatusCode", 200);
```

---

## ⚡ **Performance Guidelines**

### **Efficient Context Usage:**
```csharp
// ✅ GOOD - Reuse context for related operations
using var ctx = LogCtx.Set();
ctx.AddProperty("Operation", "BulkInsert");
ctx.AddProperty("BatchSize", items.Count);

foreach (var batch in items.Batch(100))
{
    // Add batch-specific properties to existing context
    ctx.AddProperty("BatchNumber", batchNumber++);
    LogCtx.Logger.Information("Processing batch", ctx);
    
    // Process batch...
}
```

### **Avoid Context Overhead:**
```csharp
// ❌ BAD - Creating new context for each iteration
foreach (var item in items)
{
    using var itemCtx = LogCtx.Set(); // Expensive!
    itemCtx.AddProperty("ItemId", item.Id);
    LogCtx.Logger.Debug("Processing item", itemCtx);
}

// ✅ GOOD - Batch logging with single context
using var batchCtx = LogCtx.Set();
batchCtx.AddProperty("Operation", "ProcessItems");
batchCtx.AddProperty("TotalItems", items.Count);
LogCtx.Logger.Information("Batch processing started", batchCtx);

// Process all items...
LogCtx.Logger.Information("Batch processing completed", batchCtx);
```

---

## 🧪 **Testing Patterns**

### **Integration Test Setup:**
```csharp
[TestFixture]
public class IntegrationTestBase
{
    [OneTimeSetUp]
    public void GlobalSetup()
    {
        // ⚠️ Initialize logging for all integration tests
        FailsafeLogger.Initialize("NLog.config");
        
        using var setupCtx = LogCtx.Set();
        setupCtx.AddProperty("TestSuite", GetType().Name);
        setupCtx.AddProperty("TestType", "Integration");
        LogCtx.Logger.Information("Integration test suite started", setupCtx);
    }

    [OneTimeTearDown]
    public void GlobalTearDown()
    {
        using var teardownCtx = LogCtx.Set();
        teardownCtx.AddProperty("TestSuite", GetType().Name);
        teardownCtx.AddProperty("TestType", "Integration");
        LogCtx.Logger.Information("Integration test suite completed", teardownCtx);
    }
}
```

---

## 📊 **AI Prompt Examples**

### **Example 1: Generate Service Method**
```
Generate a C# service method using LogCtx that:
- Takes userId and email parameters
- Validates email format
- Saves to database
- Returns success/failure
- Uses proper LogCtx initialization and context management
```

### **Example 2: Generate Test Class**
```
Generate NUnit test class using LogCtx that:
- Tests file upload functionality
- Includes setup/teardown with proper LogCtx initialization
- Has 3 test methods with different scenarios
- Uses Shouldly assertions
- Includes proper error logging
```

---

**Version:** 0.3.1  
**Last Updated:** October 2025  
**AI Compatibility:** ChatGPT, Claude, Copilot, Gemini