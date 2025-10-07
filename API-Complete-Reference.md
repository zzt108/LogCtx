# LogCtx API Complete Reference
*Comprehensive API documentation with examples based on actual implementation*

## 🏗️ **LogCtx Architecture Overview**

LogCtx consists of **3 shared projects** with specific interfaces and implementations:

```
LogCtx/
├── LogCtxShared/           # Core abstractions
│   ├── ILogCtxLogger.cs   # Logger interface
│   ├── IScopeContext.cs   # Context scope interface
│   ├── LogCtx.cs          # Main context manager
│   ├── Props.cs           # Property builder
│   └── JsonExtensions.cs  # JSON utilities
├── NLogShared/            # NLog implementation (PRIMARY)
│   ├── CtxLogger.cs       # NLog adapter
│   ├── FailsafeLogger.cs  # Failsafe initialization
│   └── NLogScopeContext.cs # NLog context adapter
└── SeriLogShared/         # Serilog implementation (SECONDARY)
    ├── CtxLogger.cs       # Serilog adapter
    └── SeriLogScopeContext.cs # Serilog context adapter
```

---

## 📚 **Core Classes & Interfaces**

### **LogCtx Class** *(LogCtxShared/LogCtx.cs)*

The main context manager that provides source location capture and property management.

#### **Constants**
```csharp
public const string FILE = "CTXFILE";          // File name property key
public const string LINE = "CTXLINE";          // Line number property key  
public const string METHOD = "CTXMETHOD";      // Method name property key
public const string SRC = "CTXSRC";            // Combined source location key
public const string STRACE = "CTXSTRACE";      // Stack trace property key
```

#### **Properties**
```csharp
public static bool CanLog { get; set; } = true;        // Enable/disable logging
public static ILogCtxLogger? Logger { get; private set; } // Current logger instance
```

#### **Methods**

##### **Set() - Context Creation with Automatic Source Location**
```csharp
public static Props Set(
    Props? scopeContextProps = null,
    [CallerMemberName] string memberName = "",
    [CallerFilePath] string sourceFilePath = "", 
    [CallerLineNumber] int sourceLineNumber = 0)
```

**Purpose**: Creates a new logging context with automatic source location capture.

**Parameters**:
- `scopeContextProps`: Optional initial properties
- `memberName`: Auto-captured calling method name
- `sourceFilePath`: Auto-captured source file path
- `sourceLineNumber`: Auto-captured source line number

**Returns**: `Props` object for method chaining

**Example**:
```csharp
// ✅ Basic context creation - captures file:line:method automatically
using var ctx = LogCtx.Set();
LogCtx.Logger.Info("Processing started");

// ✅ Context with initial properties
using var ctx = LogCtx.Set(new Props()
    .Add("UserId", userId)
    .Add("Operation", "ProcessFile"));
LogCtx.Logger.Info("File processing started");

// ✅ Manual source location (advanced usage)
using var ctx = LogCtx.Set(
    new Props().Add("Component", "FileProcessor"),
    "CustomMethod",
    "/custom/path/File.cs", 
    123);
```

##### **Src() - Source Location String**
```csharp
public static string Src(
    string message,
    [CallerMemberName] string memberName = "",
    [CallerFilePath] string sourceFilePath = "",
    [CallerLineNumber] int sourceLineNumber = 0)
```

**Purpose**: Generates a formatted source location string.

**Returns**: String in format "FileName.MethodName:LineNumber"

**Example**:
```csharp
// Generates: "MyService.ProcessFile:42"
var location = LogCtx.Src("Processing file");
```

---

### **Props Class** *(LogCtxShared/Props.cs)*

Fluent property builder that extends `Dictionary<string, object>` with method chaining.

#### **Constructors**

##### **Default Constructor**
```csharp
public Props()
```

##### **Parameterized Constructor**
```csharp
public Props(params object[] args)
```

**Purpose**: Creates Props with alternating key-value pairs.

**Example**:
```csharp
// Creates: {"UserId": 123, "Action": "Login", "Success": true}
var props = new Props("UserId", 123, "Action", "Login", "Success", true);
```

#### **Methods**

##### **Add() - Basic Property Addition**
```csharp
public Props Add(string key, object? value)
```

**Purpose**: Adds or updates a property with fluent interface.

**Example**:
```csharp
var props = new Props()
    .Add("UserId", 123)
    .Add("Timestamp", DateTime.UtcNow)
    .Add("Success", true)
    .Add("Duration", 1234);
```

##### **AddJson() - JSON Serialization**  
```csharp
public Props AddJson(string key, object value)
```

**Purpose**: Serializes value to JSON before storing.

**Example**:
```csharp
var props = new Props()
    .AddJson("UserData", new { Name = "John", Age = 30 })
    .AddJson("Config", configObject);
```

##### **Clear() - Reset Properties**
```csharp
public Props Clear()
```

**Purpose**: Removes all properties, returns self for chaining.

**Example**:
```csharp
var props = new Props()
    .Add("Key1", "Value1")
    .Clear() // Now empty
    .Add("Key2", "Value2"); // Only Key2 remains
```

---

### **ILogCtxLogger Interface** *(LogCtxShared/ILogCtxLogger.cs)*

Logger abstraction that supports both NLog and Serilog backends.

#### **Properties**
```csharp
LogCtx Ctx { get; set; }  // Current context instance
```

#### **Configuration Methods**
```csharp
bool ConfigureXml(string? configPath);    // Configure from XML file
bool ConfigureJson(string configPath);   // Configure from JSON file  
```

#### **Logging Methods**
```csharp
void Trace(string message);                    // Trace level
void Debug(string message);                    // Debug level
void Info(string message);                     // Information level
void Warn(string message);                     // Warning level
void Error(Exception ex, string message);     // Error with exception
void Fatal(Exception ex, string message);     // Fatal with exception
```

---

## 🎪 **NLog Implementation** *(NLogShared/)*

### **CtxLogger Class** *(NLogShared/CtxLogger.cs)*

Primary logger implementation using NLog backend.

#### **Initialization**
```csharp
public CtxLogger()
{
    ConfigureXml(logConfigPath);
    Logger = LogManager.GetCurrentClassLogger();
    Ctx = new LogCtx(new NLogScopeContext());
}
```

#### **Configuration Methods**

##### **ConfigureXml() - XML Configuration**
```csharp
public bool ConfigureXml(string? configPath)
```

**Purpose**: Configures NLog from XML configuration file.

**Example**:
```csharp
var logger = new CtxLogger();
var success = logger.ConfigureXml("NLog.config");
if (!success) {
    // Handle configuration failure
}
```

##### **ConfigureJson() - JSON Configuration (Not Implemented)**
```csharp
public bool ConfigureJson(string configPath)
{
    throw new NotImplementedException("Only XML configuration is supported");
}
```

#### **Logging Implementation**
```csharp
public void Debug(string message) => Logger?.Debug(message);
public void Info(string message) => Logger?.Info(message);  
public void Warn(string message) => Logger?.Warn(message);
public void Error(Exception ex, string message) => Logger?.Error(ex, message);
public void Fatal(Exception ex, string message) => Logger?.Fatal(ex, message);
```

---

### **FailsafeLogger Class** *(NLogShared/FailsafeLogger.cs)*

Robust initialization that never throws exceptions.

#### **Initialize() - Failsafe Initialization**
```csharp
public static bool Initialize(
    string? preferredFileName = "NLog.config",
    string? altJsonFileName = "NLog.json")
```

**Purpose**: Attempts multiple configuration methods, falls back to minimal config if all fail.

**Logic Flow**:
1. Try XML configuration from `AppContext.BaseDirectory`
2. Try JSON configuration (fallback)
3. Apply minimal in-memory configuration (console + file)
4. Apply no-op configuration (last resort)

**Returns**: Always `true` (never throws)

**Example**:
```csharp
// ✅ Standard initialization
var success = FailsafeLogger.Initialize();

// ✅ Custom configuration files
var success = FailsafeLogger.Initialize("MyApp.config", "MyApp.json");

// ✅ In Program.cs
static void Main(string[] args)
{
    FailsafeLogger.Initialize();  // Never throws, always works
    
    // Your application logic...
}
```

#### **Fallback Configurations**

##### **Minimal Fallback Configuration**
When no config files found, creates:
- Console target for immediate feedback
- Rolling file target in `Logs/` directory
- 50MB file size limit with archiving

##### **No-Op Fallback Configuration**  
When even minimal config fails, creates:
- Null target that discards all logs
- Ensures application never crashes from logging issues

---

### **NLogScopeContext Class** *(NLogShared/CtxLogger.cs)*

NLog-specific context implementation.

#### **Methods**
```csharp
public void Clear()                            // Clear NLog.ScopeContext
public void PushProperty(string key, object value)  // Add property to NLog context
```

**Implementation**:
```csharp
public class NLogScopeContext : IScopeContext
{
    public void Clear() => NLog.ScopeContext.Clear();
    public void PushProperty(string key, object value) => 
        NLog.ScopeContext.PushProperty(key, value);
}
```

---

## 🔍 **Serilog Implementation** *(SeriLogShared/)*

### **CtxLogger Class** *(SeriLogShared/CtxLogger.cs)*

Secondary logger implementation using Serilog backend.

#### **Configuration Methods**

##### **ConfigureJson() - JSON Configuration**
```csharp
public bool ConfigureJson(string configPath)
{
    configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile(configPath)
        .Build();
        
    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(configuration)
        .CreateLogger();
        
    return true;
}
```

##### **ConfigureXml() - XML Configuration**  
```csharp
public bool ConfigureXml(string configPath)
{
    configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddXmlFile(configPath)
        .Build();
        
    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(configuration)
        .CreateLogger();
        
    return true;
}
```

#### **Logging Implementation**
```csharp
public void Debug(string message) => Log.Debug(message);
public void Info(string message) => Log.Information(message);
public void Warn(string message) => Log.Warning(message);
public void Trace(string message) => Log.Verbose(message);
public void Error(Exception ex, string message) => Log.Error(ex, message);
public void Fatal(Exception ex, string message) => Log.Fatal(ex, message);
```

#### **Disposal**
```csharp
public void Dispose() => Log.CloseAndFlush();
```

---

### **SeriLogScopeContext Class** *(SeriLogShared/CtxLogger.cs)*

Serilog-specific context implementation.

#### **Methods**
```csharp
public class SeriLogScopeContext : IScopeContext  
{
    public void Clear() => LogContext.Reset();
    public void PushProperty(string key, object value) => 
        LogContext.PushProperty(key, value);
}
```

---

## 🛠️ **JsonExtensions Class** *(LogCtxShared/JsonExtensions.cs)*

JSON utility methods for object serialization.

#### **Methods**

##### **AsJson() - Object Serialization**
```csharp
public static string AsJson(this object obj, bool indented = false)
```

**Purpose**: Converts any object to JSON string.

**Example**:
```csharp
var user = new { Id = 123, Name = "John", Active = true };
var json = user.AsJson();           // Compact JSON
var prettyJson = user.AsJson(true); // Indented JSON
```

##### **AsJsonDiagram() - PlantUML Integration**
```csharp
public static string AsJsonDiagram(this object obj)
```

**Purpose**: Wraps JSON in PlantUML diagram format.

**Returns**: `@startjson ClassName\n{...}\n@endjson`

**Example**:
```csharp
var config = new AppConfig { Debug = true, Port = 8080 };
var diagram = config.AsJsonDiagram();
// Output: @startjson AppConfig\n{"Debug":true,"Port":8080}\n@endjson
```

##### **AsJsonEmbedded() - PlantUML Embedded**
```csharp
public static string AsJsonEmbedded(this object obj)
```

**Purpose**: Creates embedded JSON for PlantUML diagrams.

**Returns**: `json ClassName as J {...}`

##### **FromJson<T>() - Deserialization**
```csharp
public static T FromJson<T>(string value)
```

**Purpose**: Deserializes JSON string to typed object.

**Example**:
```csharp
var json = """{"Id":123,"Name":"John","Active":true}""";
var user = json.FromJson<User>();
```

---

## 🎯 **Usage Patterns & Examples**

### **Pattern 1: Basic Method Logging**
```csharp
public async Task<bool> ProcessFileAsync(string filePath)
{
    // ✅ Context captures method name, file, and line automatically
    using var ctx = LogCtx.Set(new Props()
        .Add("FilePath", filePath)
        .Add("Operation", "ProcessFile"));
        
    LogCtx.Logger.Info("File processing started");
    
    try
    {
        var content = await File.ReadAllTextAsync(filePath);
        ctx.Add("FileSize", content.Length);
        
        // Process file content...
        
        LogCtx.Logger.Info("File processing completed successfully");
        return true;
    }
    catch (Exception ex)
    {
        ctx.Add("ErrorType", ex.GetType().Name);
        LogCtx.Logger.Error(ex, "File processing failed");
        return false;
    }
}
```

### **Pattern 2: Performance Monitoring**
```csharp
public async Task<List<T>> ProcessBatchAsync<T>(List<T> items)
{
    var stopwatch = Stopwatch.StartNew();
    
    using var ctx = LogCtx.Set(new Props()
        .Add("BatchSize", items.Count)
        .Add("ItemType", typeof(T).Name)
        .Add("StartTime", DateTime.UtcNow));
        
    LogCtx.Logger.Info("Batch processing started");
    
    var results = new List<T>();
    var processed = 0;
    
    foreach (var item in items)
    {
        // Process each item...
        results.Add(ProcessSingleItem(item));
        processed++;
        
        // Log progress every 100 items
        if (processed % 100 == 0)
        {
            ctx.Add("ProcessedCount", processed);
            ctx.Add("ElapsedMs", stopwatch.ElapsedMilliseconds);
            LogCtx.Logger.Debug("Batch progress update");
        }
    }
    
    stopwatch.Stop();
    
    ctx.Add("TotalProcessed", processed);
    ctx.Add("TotalDuration", stopwatch.ElapsedMilliseconds);
    ctx.Add("AverageItemDuration", stopwatch.ElapsedMilliseconds / items.Count);
    
    LogCtx.Logger.Info("Batch processing completed");
    
    return results;
}
```

### **Pattern 3: Error Context Enrichment**
```csharp
public async Task<ApiResponse> CallExternalApiAsync(string endpoint, object data)
{
    using var ctx = LogCtx.Set(new Props()
        .Add("Endpoint", endpoint)
        .Add("RequestSize", data.AsJson().Length)
        .Add("Timestamp", DateTime.UtcNow));
    
    LogCtx.Logger.Info("External API call initiated");
    
    try
    {
        using var httpClient = new HttpClient();
        var json = data.AsJson();
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await httpClient.PostAsync(endpoint, content);
        
        ctx.Add("StatusCode", (int)response.StatusCode);
        ctx.Add("ResponseSize", response.Content.Headers.ContentLength ?? 0);
        
        if (response.IsSuccessStatusCode)
        {
            LogCtx.Logger.Info("External API call successful");
            var responseText = await response.Content.ReadAsStringAsync();
            return responseText.FromJson<ApiResponse>();
        }
        else
        {
            var errorText = await response.Content.ReadAsStringAsync();
            ctx.Add("ErrorResponse", errorText);
            LogCtx.Logger.Warn("External API call failed with non-success status");
            throw new HttpRequestException($"API call failed: {response.StatusCode}");
        }
    }
    catch (HttpRequestException ex)
    {
        ctx.Add("ErrorType", "HttpRequestException");
        ctx.Add("HttpError", ex.Message);
        LogCtx.Logger.Error(ex, "HTTP request failed");
        throw;
    }
    catch (TaskCanceledException ex)
    {
        ctx.Add("ErrorType", "TaskCanceledException");
        ctx.Add("TimeoutError", ex.Message);
        LogCtx.Logger.Error(ex, "API request timed out");
        throw;
    }
    catch (Exception ex)
    {
        ctx.Add("ErrorType", ex.GetType().Name);
        ctx.Add("UnexpectedError", ex.Message);
        LogCtx.Logger.Error(ex, "Unexpected error during API call");
        throw;
    }
}
```

### **Pattern 4: Testing Integration**
```csharp
[TestFixture]
public class FileProcessorTests
{
    [OneTimeSetUp]
    public void Setup()
    {
        // ✅ Initialize LogCtx once for test suite
        FailsafeLogger.Initialize();
    }
    
    [Test]
    public async Task ProcessFile_ValidInput_ShouldSucceed()
    {
        // Arrange
        using var testCtx = LogCtx.Set(new Props()
            .Add("TestMethod", nameof(ProcessFile_ValidInput_ShouldSucceed))
            .Add("TestCategory", "FileProcessing")
            .Add("TestStartTime", DateTime.UtcNow));
            
        LogCtx.Logger.Info("Test execution started");
        
        var processor = new FileProcessor();
        var testFilePath = "test-data/sample.txt";
        
        // Act
        var result = await processor.ProcessFileAsync(testFilePath);
        
        // Assert
        result.ShouldBeTrue();
        
        testCtx.Add("TestResult", "Success");
        testCtx.Add("TestEndTime", DateTime.UtcNow);
        LogCtx.Logger.Info("Test execution completed successfully");
    }
    
    [Test]
    public async Task ProcessFile_InvalidFile_ShouldThrowException()
    {
        // Arrange
        using var testCtx = LogCtx.Set(new Props()
            .Add("TestMethod", nameof(ProcessFile_InvalidFile_ShouldThrowException))
            .Add("TestCategory", "ErrorHandling"));
            
        LogCtx.Logger.Info("Exception test started");
        
        var processor = new FileProcessor();
        var invalidPath = "non-existent-file.txt";
        
        // Act & Assert
        var exception = await Should.ThrowAsync<FileNotFoundException>(
            () => processor.ProcessFileAsync(invalidPath));
        
        testCtx.Add("ExpectedException", exception.GetType().Name);
        LogCtx.Logger.Info("Exception test completed successfully");
    }
}
```

---

## 🎯 **Key Takeaways**

1. **Automatic Source Location**: `LogCtx.Set()` captures file, method, and line automatically
2. **Fluent Property Building**: `Props` class supports method chaining for readable code
3. **Failsafe Initialization**: `FailsafeLogger.Initialize()` never throws, always works
4. **Structured Logging**: Properties are structured for powerful SEQ queries
5. **Multiple Backends**: Supports both NLog (primary) and Serilog (secondary)
6. **Exception Context**: Easy error context enrichment with relevant properties
7. **Performance Tracking**: Built-in support for timing and performance metrics
8. **Testing Integration**: Simple setup for unit and integration tests

**Next Steps**: See [SEQ-Configuration-Guide.md](SEQ-Configuration-Guide.md) for complete SEQ integration! 🚀