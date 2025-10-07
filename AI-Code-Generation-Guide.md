# LogCtx AI Code Generation Guide
*Optimized for AI coding assistants: GitHub Copilot, ChatGPT, Claude, Gemini, Perplexity*

## 🎯 **Quick Start - Copy-Paste Ready**

### **Basic LogCtx Usage Pattern**
```csharp
using LogCtx;
using NLog;

public class ExampleService
{
    private static readonly Logger log = LogManager.GetCurrentClassLogger();

    public async Task ProcessFileAsync(string filePath)
    {
        // ✅ Step 1: Wrap significant actions in LogCtx.Set
        using var ctx1 = LogCtx.Set(); // Captures file/line automatically
        LogCtx.LogInformation("STEP 1: Starting file processing", ctx1);
        
        try
        {
            var content = await File.ReadAllTextAsync(filePath);
            
            // ✅ Step 2: New context for validation
            using var ctx2 = LogCtx.Set();
            ctx2.AddProperty("FileSize", content.Length);
            ctx2.AddProperty("FilePath", filePath);
            LogCtx.LogInformation("STEP 2: File loaded successfully", ctx2);
            
            // Process content...
            
        }
        catch (Exception ex)
        {
            // ✅ Step 3: Error context with enrichment
            using var ctxErr = LogCtx.Set();
            ctxErr.AddProperty("FilePath", filePath);
            ctxErr.AddProperty("Operation", "ProcessFile");
            LogCtx.LogError("File processing failed", ex, ctxErr);
            throw;
        }
    }
}
```

## 🚀 **Essential Patterns**

### **1. Per-Action Context Pattern**
```csharp
// ❌ DON'T: Reuse contexts across operations
using var ctx = LogCtx.Set();
LogCtx.LogInformation("Step 1", ctx);
DoSomething();
LogCtx.LogInformation("Step 2", ctx); // Wrong - different location!

// ✅ DO: Fresh context per significant step
using var ctx1 = LogCtx.Set();
LogCtx.LogInformation("Step 1: Starting validation", ctx1);

DoSomething();

using var ctx2 = LogCtx.Set(); 
LogCtx.LogInformation("Step 2: Validation complete", ctx2);
```

### **2. Loop/Batch Processing Pattern**
```csharp
public async Task ProcessBatchAsync(List<string> items)
{
    using var batchCtx = LogCtx.Set();
    batchCtx.AddProperty("BatchSize", items.Count);
    LogCtx.LogInformation("Starting batch processing", batchCtx);
    
    foreach (var item in items)
    {
        // ✅ Nested context for each item
        using var itemCtx = LogCtx.Set();
        itemCtx.AddProperty("Item", item);
        itemCtx.AddProperty("BatchSize", items.Count);
        
        try
        {
            await ProcessSingleItemAsync(item);
            LogCtx.LogDebug("Item processed successfully", itemCtx);
        }
        catch (Exception ex)
        {
            LogCtx.LogError("Item processing failed", ex, itemCtx);
            // Continue with next item...
        }
    }
}
```

### **3. Error Handling with Context**
```csharp
public async Task<bool> TryOperationAsync(string parameter)
{
    using var ctx = LogCtx.Set();
    ctx.AddProperty("Parameter", parameter);
    ctx.AddProperty("Operation", nameof(TryOperationAsync));
    
    try
    {
        LogCtx.LogInformation("Operation starting", ctx);
        
        // Your logic here...
        await SomeRiskyOperationAsync(parameter);
        
        LogCtx.LogInformation("Operation completed successfully", ctx);
        return true;
    }
    catch (TimeoutException ex)
    {
        ctx.AddProperty("TimeoutSeconds", ex.Data["Timeout"]);
        LogCtx.LogWarning("Operation timed out", ex, ctx);
        return false;
    }
    catch (Exception ex)
    {
        ctx.AddProperty("ErrorType", ex.GetType().Name);
        LogCtx.LogError("Operation failed unexpectedly", ex, ctx);
        return false;
    }
}
```

### **4. Testing Pattern with LogCtx**
```csharp
[TestFixture]
public class ServiceTests
{
    [SetUp]
    public void Setup()
    {
        // ✅ Initialize LogCtx once per test fixture
        LogCtx.InitLogCtx();
    }
    
    [Test]
    public async Task ProcessFile_ValidInput_ShouldSucceed()
    {
        // Arrange
        using var testCtx = LogCtx.Set();
        testCtx.AddProperty("TestMethod", nameof(ProcessFile_ValidInput_ShouldSucceed));
        LogCtx.LogInformation("Test starting", testCtx);
        
        var service = new FileProcessorService();
        var testFile = "test-file.txt";
        
        // Act
        var result = await service.ProcessFileAsync(testFile);
        
        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        
        LogCtx.LogInformation("Test completed successfully", testCtx);
    }
}
```

## 🎯 **SEQ Integration (Primary Target)**

### **NLog.config for SEQ**
```xml
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  
  <targets>
    <!-- SEQ Target with structured logging -->
    <target xsi:type="Seq" 
            name="seq" 
            serverUrl="http://localhost:5341"
            apiKey="${environment:SEQ_API_KEY}"
            compactMode="true">
      
      <property name="Application" value="YourAppName" />
      <property name="Environment" value="${environment:ASPNETCORE_ENVIRONMENT:default=Development}" />
      <property name="MachineName" value="${machinename}" />
      <property name="ProcessId" value="${processid}" />
      
    </target>
    
    <!-- Console fallback -->
    <target xsi:type="Console" 
            name="console"
            layout="${time} [${level:uppercase=true}] ${logger}: ${message} ${exception:format=tostring}" />
  </targets>
  
  <rules>
    <logger name="*" minlevel="Debug" writeTo="seq" />
    <logger name="*" minlevel="Info" writeTo="console" />
  </rules>
  
</nlog>
```

### **Program.cs Initialization**
```csharp
using LogCtx;
using NLog;

var builder = WebApplication.CreateBuilder(args);

// ✅ Initialize LogCtx early in application lifecycle
LogCtx.InitLogCtx();

// Configure NLog
LogManager.Configuration = new NLogLoggingConfiguration(builder.Configuration.GetSection("NLog"));

var app = builder.Build();

// ✅ Log application startup with context
using var startupCtx = LogCtx.Set();
startupCtx.AddProperty("Environment", builder.Environment.EnvironmentName);
startupCtx.AddProperty("ApplicationName", builder.Environment.ApplicationName);
LogCtx.LogInformation("Application starting up", startupCtx);

app.Run();
```

## 🔧 **Common Property Patterns**

### **Standard Properties to Add**
```csharp
// ✅ File operations
ctx.AddProperty("FilePath", filePath);
ctx.AddProperty("FileSize", fileInfo.Length);
ctx.AddProperty("Operation", "FileRead");

// ✅ Network operations  
ctx.AddProperty("Url", requestUrl);
ctx.AddProperty("HttpMethod", "GET");
ctx.AddProperty("TimeoutMs", timeoutMilliseconds);

// ✅ Database operations
ctx.AddProperty("TableName", "Users");
ctx.AddProperty("RecordId", userId);
ctx.AddProperty("QueryDuration", stopwatch.ElapsedMilliseconds);

// ✅ User context
ctx.AddProperty("UserId", currentUser.Id);
ctx.AddProperty("UserRole", currentUser.Role);
ctx.AddProperty("SessionId", sessionContext.Id);

// ✅ Performance tracking
ctx.AddProperty("ExecutionTimeMs", stopwatch.ElapsedMilliseconds);
ctx.AddProperty("ItemCount", items.Count);
ctx.AddProperty("BatchSize", batchSize);
```

## ⚠️ **Critical Don'ts**

### **❌ DON'T: Direct NLog Usage**
```csharp
// ❌ WRONG: Direct NLog calls lose LogCtx benefits
private static readonly Logger log = LogManager.GetCurrentClassLogger();
log.Info("Something happened"); // No context, no source location
```

### **❌ DON'T: Context Reuse**  
```csharp
// ❌ WRONG: Reusing context across different operations
using var ctx = LogCtx.Set();
LogCtx.LogInfo("Step 1", ctx);
DoSomething();
LogCtx.LogInfo("Step 2", ctx); // Wrong file/line captured!
```

### **❌ DON'T: Empty Contexts**
```csharp
// ❌ WRONG: Not enriching context with useful data
using var ctx = LogCtx.Set();
LogCtx.LogInfo("Processing complete", ctx); // No context about WHAT was processed
```

## ✅ **Best Practices Summary**

1. **One Context Per Action**: `using var ctx = LogCtx.Set()` for each significant step
2. **Enrich Before Logging**: Always add relevant properties before logging
3. **Initialize Once**: Call `LogCtx.InitLogCtx()` once per application/test fixture
4. **Structured Properties**: Use meaningful property names and values
5. **Exception Context**: Always add context properties before logging errors
6. **Consistent Naming**: Use PascalCase for property names
7. **Performance Data**: Include timing and count information where relevant

## 🎯 **AI Assistant Integration Tips**

### **For GitHub Copilot**
- Type `using var ctx = LogCtx.Set();` and Copilot will suggest the logging pattern
- Start method names with logged actions: `ProcessFile`, `ValidateData`, `SendRequest`

### **For ChatGPT/Claude/Gemini**
- Share this guide and say: "Follow LogCtx patterns from the guide"
- Ask for: "Generate LogCtx-compliant service method for [operation]"

### **For Code Reviews**
- Check: Every significant operation has its own `LogCtx.Set()`
- Check: Context properties are added before logging
- Check: Error contexts include operation details

---

**Confidence Level: 10/10** - This guide covers all essential LogCtx patterns for AI-assisted development! 🎯