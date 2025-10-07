# LogCtx Troubleshooting Guide
*Comprehensive problem-solving guide for common LogCtx issues and solutions*

## 🎯 **Troubleshooting Philosophy**

LogCtx is designed to be **failsafe** - it should never crash your application, even when logging fails. This guide addresses common issues based on real VecTool production experiences and provides systematic solutions.

### **Troubleshooting Principles**
- **Failsafe First**: LogCtx uses defensive programming to prevent crashes
- **Systematic Diagnosis**: Follow structured debugging steps
- **Environment-Specific**: Different solutions for Development vs Production
- **SEQ-Optimized**: Focus on structured logging and SEQ integration issues

---

## 🚨 **Critical Issues (Application-Breaking)**

### **Issue 1: LogCtx Not Initializing**

**Symptoms:**
- `LogCtx.Logger` is null
- No logs appear in any target
- Application crashes on first `LogCtx.Logger.Info()` call

**Root Cause Analysis:**
```csharp
// ❌ WRONG: Never initialized LogCtx
public class Program
{
    static void Main()
    {
        // Missing initialization!
        LogCtx.Logger.Info("This will crash!"); // NullReferenceException
    }
}
```

**✅ Solution:**
```csharp
// ✅ CORRECT: Always initialize first
using NLogShared;

public class Program
{
    static void Main()
    {
        // Step 1: Initialize LogCtx (failsafe - never throws)
        FailsafeLogger.Initialize("NLog.config");
        
        // Step 2: Verify initialization succeeded
        if (LogCtx.Logger == null)
        {
            Console.WriteLine("ERROR: LogCtx failed to initialize!");
            Environment.Exit(1);
        }
        
        // Step 3: Now safe to use
        using var ctx = LogCtx.Set(new Props().Add("Application", "MyApp"));
        LogCtx.Logger.Info("Application started successfully");
    }
}
```

**Diagnostic Steps:**
1. Check if `FailsafeLogger.Initialize()` was called
2. Verify NLog.config exists and is valid XML
3. Check NLog internal logs for errors
4. Test with minimal configuration

### **Issue 2: Shared Project Import Failures**

**Symptoms:**
- `LogCtxShared` types not found
- Build errors: "The name 'LogCtx' does not exist"
- IntelliSense doesn't show LogCtx classes

**Root Cause Analysis:**
```xml
<!-- ❌ WRONG: Incorrect shared project import -->
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <!-- This will fail - incorrect path -->
    <Import Project="LogCtx\LogCtxShared.projitems" Label="Shared" />
  </ItemGroup>
</Project>
```

**✅ Solution:**
```xml
<!-- ✅ CORRECT: Proper shared project imports -->
<Project Sdk="Microsoft.NET.Sdk">
  
  <!-- Import core LogCtx shared project -->
  <Import Project="LogCtx\LogCtxShared\LogCtxShared.projitems" Label="Shared" />
  
  <!-- Import NLog implementation (choose one) -->
  <Import Project="LogCtx\NLogShared\NLogShared.projitems" Label="Shared" />
  
  <!-- OR Import Serilog implementation (alternative) -->
  <!-- <Import Project="LogCtx\SeriLogShared\SeriLogShared.projitems" Label="Shared" /> -->
  
  <ItemGroup>
    <!-- Required NLog packages -->
    <PackageReference Include="NLog" Version="5.3.4" />
    <PackageReference Include="NLog.Targets.Seq" Version="4.0.2" />
  </ItemGroup>
  
</Project>
```

**Diagnostic Steps:**
1. Verify Git submodule initialization: `git submodule update --init --recursive`
2. Check shared project file paths in solution
3. Rebuild solution completely
4. Clear NuGet cache and restore packages

---

## 🔧 **Configuration Issues**

### **Issue 3: NLog.config Not Found**

**Symptoms:**
- Logs go to console but not to SEQ or files
- Internal NLog errors about missing configuration
- FailsafeLogger falls back to minimal config

**Root Cause Analysis:**
```
MyApp/
├── Program.cs
├── MyApp.csproj
└── Config/            # Wrong location!
    └── NLog.config
```

**✅ Solution:**
```
MyApp/
├── Program.cs
├── MyApp.csproj
└── NLog.config        # Must be in root or bin/Debug
```

**Alternative Configuration:**
```csharp
// Specify custom config path
FailsafeLogger.Initialize("Config/NLog.config");

// Or use environment-specific configs
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
var configFile = $"NLog.{environment}.config";
FailsafeLogger.Initialize(configFile);
```

**Diagnostic NLog.config:**
```xml
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
      autoReload="true"
      throwExceptions="false"
      internalLogLevel="Warn"
      internalLogFile="Logs/nlog-internal.log">
  
  <targets>
    <!-- Diagnostic: Always include console for immediate feedback -->
    <target name="console" 
            xsi:type="ColoredConsole"
            layout="${time} [${level}] ${message}" />
  </targets>
  
  <rules>
    <logger name="*" minlevel="Info" writeTo="console" />
  </rules>
  
</nlog>
```

### **Issue 4: SEQ Server Connectivity Issues**

**Symptoms:**
- Logs appear in console/files but not in SEQ
- SEQ dashboard shows no recent logs
- Timeouts or connection refused errors

**Root Cause Analysis:**
```xml
<!-- ❌ WRONG: Incorrect SEQ configuration -->
<target name="seq" 
        xsi:type="Seq" 
        serverUrl="http://seq.company.com:5341"  <!-- Wrong URL -->
        apiKey="${environment:SEQ_API_KEY}">     <!-- Missing API key -->
</target>
```

**✅ Solution - Step-by-Step SEQ Diagnosis:**

**Step 1: Verify SEQ Server**
```bash
# Test SEQ server connectivity
curl -I http://localhost:5341/api/events/raw

# Expected response:
# HTTP/1.1 201 Created
```

**Step 2: Test Basic SEQ Target**
```xml
<target name="seq-test" 
        xsi:type="Seq" 
        serverUrl="http://localhost:5341">
  <!-- No API key for testing -->
  <property name="Application" value="TestApp" />
</target>
```

**Step 3: Add Diagnostic Logging**
```xml
<nlog internalLogLevel="Debug" 
      internalLogFile="Logs/nlog-internal.log">
  <!-- NLog will log connection attempts to internal log -->
</nlog>
```

**Step 4: Production SEQ Configuration**
```xml
<target name="seq" 
        xsi:type="BufferingWrapper"
        bufferSize="1000"
        flushTimeout="2000">
  
  <target xsi:type="Seq" 
          serverUrl="${environment:SEQ_SERVER_URL:default=http://localhost:5341}"
          apiKey="${environment:SEQ_API_KEY}">
    
    <property name="Application" value="VecTool" />
    <property name="Environment" value="${environment:ASPNETCORE_ENVIRONMENT:default=Development}" />
    <property name="MachineName" value="${machinename}" />
  </target>
</target>
```

**Environment Variables:**
```bash
# For development
export SEQ_SERVER_URL="http://localhost:5341"

# For production
export SEQ_SERVER_URL="https://seq.company.com"
export SEQ_API_KEY="your-production-api-key"
```

### **Issue 5: Structured Properties Not Appearing in SEQ**

**Symptoms:**
- Logs appear in SEQ but as plain text messages
- No structured properties for filtering/querying
- SEQ queries return no results

**Root Cause Analysis:**
```csharp
// ❌ WRONG: String interpolation loses structure
LogCtx.Logger.Info($"Processing file {filePath} with {itemCount} items");

// ❌ WRONG: Not using LogCtx context
LogCtx.Logger.Info("Processing file");
```

**✅ Solution:**
```csharp
// ✅ CORRECT: Use LogCtx context with structured properties
using var ctx = LogCtx.Set(new Props()
    .Add("FilePath", filePath)
    .Add("ItemCount", itemCount)
    .Add("Operation", "ProcessFile"));
    
LogCtx.Logger.Info("Processing file started");

// Result in SEQ: 
// - Message: "Processing file started"
// - Properties: FilePath="test.txt", ItemCount=42, Operation="ProcessFile"
```

**SEQ Query Testing:**
```sql
-- Test if properties are structured
FilePath is not null

-- Test specific property values
Operation = 'ProcessFile'

-- Test numeric properties
ItemCount > 10
```

---

## 🧪 **Testing Issues**

### **Issue 6: LogCtx Not Working in Tests**

**Symptoms:**
- Tests pass but no log output appears
- NullReferenceException in test execution
- Missing test context in SEQ

**Root Cause Analysis:**
```csharp
// ❌ WRONG: LogCtx not initialized in tests
[TestFixture]
public class MyServiceTests
{
    [Test]
    public void ProcessFile_ShouldSucceed()
    {
        // Missing FailsafeLogger.Initialize()!
        var service = new MyService();
        service.ProcessFile("test.txt"); // Will fail if service uses LogCtx
    }
}
```

**✅ Solution:**
```csharp
// ✅ CORRECT: Initialize LogCtx once per test fixture
[TestFixture]
public class MyServiceTests
{
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        // Initialize LogCtx for all tests in this fixture
        FailsafeLogger.Initialize(); // Uses minimal config if no NLog.config found
    }
    
    [Test]
    public void ProcessFile_ShouldSucceed()
    {
        // Arrange
        using var testCtx = LogCtx.Set(new Props()
            .Add("TestMethod", nameof(ProcessFile_ShouldSucceed))
            .Add("TestCategory", "FileProcessing"));
            
        LogCtx.Logger.Info("Test execution started");
        
        // Act
        var service = new MyService();
        var result = service.ProcessFile("test.txt");
        
        // Assert
        result.ShouldBeTrue();
        LogCtx.Logger.Info("Test execution completed");
    }
}
```

**Test-Specific NLog.config:**
```xml
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd">
  <targets>
    <!-- Console for test runner output -->
    <target name="console" 
            xsi:type="Console"
            layout="[${level}] ${message}" />
            
    <!-- Memory for test assertions -->
    <target name="memory" 
            xsi:type="Memory"
            layout="${level}|${message}" />
  </targets>
  
  <rules>
    <logger name="*" minlevel="Debug" writeTo="console,memory" />
  </rules>
</nlog>
```

### **Issue 7: STA Thread Issues in WinForms Tests**

**Symptoms:**
- COM exceptions during drag-drop tests
- "Current thread must be set to single thread apartment" errors
- UI tests fail intermittently

**Root Cause Analysis:**
```csharp
// ❌ WRONG: Missing STA configuration
[TestFixture]
public class WinFormsTests
{
    [Test]
    public void DragDrop_ShouldWork()
    {
        var panel = new RecentFilesPanel();
        // This will fail with COM exceptions
    }
}
```

**✅ Solution:**
```csharp
// ✅ CORRECT: Configure STA for WinForms tests
// In AssemblyInfo.cs or AssemblyAttributes.cs
using NUnit.Framework;

[assembly: Apartment(ApartmentState.STA)]
[assembly: LevelOfParallelism(1)]  // Single worker for STA

[TestFixture]
public class WinFormsTests
{
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        FailsafeLogger.Initialize();
        
        // Ensure we're on STA thread
        if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
        {
            Assert.Fail("Tests must run on STA thread for WinForms components");
        }
    }
    
    [Test]
    public void DragDrop_ShouldWork()
    {
        using var testCtx = LogCtx.Set(new Props()
            .Add("TestMethod", nameof(DragDrop_ShouldWork))
            .Add("ThreadApartment", Thread.CurrentThread.GetApartmentState().ToString()));
            
        var panel = new RecentFilesPanel();
        // Now safe for drag-drop operations
    }
}
```

---

## 🚀 **Performance Issues**

### **Issue 8: LogCtx Performance Degradation**

**Symptoms:**
- Application becomes slow over time
- High memory usage related to logging
- Long delays during log-heavy operations

**Root Cause Analysis:**
```csharp
// ❌ WRONG: Context abuse in tight loops
public void ProcessLargeDataset(List<string> items)
{
    foreach (var item in items) // 10,000+ items
    {
        // Creating context per item is expensive!
        using var ctx = LogCtx.Set(new Props().Add("Item", item));
        LogCtx.Logger.Debug($"Processing {item}");
        // Heavy object allocation and disposal
    }
}
```

**✅ Solution:**
```csharp
// ✅ CORRECT: Batch logging approach
public void ProcessLargeDataset(List<string> items)
{
    // Single context for the entire operation
    using var ctx = LogCtx.Set(new Props()
        .Add("Operation", "ProcessLargeDataset")
        .Add("TotalItems", items.Count)
        .Add("StartTime", DateTime.UtcNow));
        
    LogCtx.Logger.Info("Large dataset processing started");
    
    var processed = 0;
    var stopwatch = Stopwatch.StartNew();
    
    foreach (var item in items)
    {
        ProcessSingleItem(item);
        processed++;
        
        // Log progress every 1000 items
        if (processed % 1000 == 0)
        {
            ctx.Add("ProcessedCount", processed);
            ctx.Add("ElapsedMs", stopwatch.ElapsedMilliseconds);
            LogCtx.Logger.Debug("Batch progress update");
        }
    }
    
    ctx.Add("FinalCount", processed);
    ctx.Add("TotalDuration", stopwatch.ElapsedMilliseconds);
    LogCtx.Logger.Info("Large dataset processing completed");
}
```

**High-Performance SEQ Configuration:**
```xml
<target name="seq" 
        xsi:type="AsyncWrapper"
        queueLimit="10000"
        batchSize="1000"
        timeToSleepBetweenBatches="50">
  
  <target name="seqBuffer" 
          xsi:type="BufferingWrapper"
          bufferSize="5000"
          flushTimeout="1000">
    
    <target xsi:type="Seq" 
            serverUrl="http://localhost:5341">
      <property name="Application" value="HighVolumeApp" />
    </target>
  </target>
</target>
```

### **Issue 9: Memory Leaks in Long-Running Applications**

**Symptoms:**
- Memory usage grows continuously
- OutOfMemoryException after hours/days of operation
- High GC pressure

**Root Cause Analysis:**
```csharp
// ❌ WRONG: Not disposing contexts properly
public class LongRunningService
{
    private Props globalContext; // This accumulates!
    
    public void ProcessRequest(string request)
    {
        globalContext ??= new Props();
        globalContext.Add("Request", request); // Memory leak!
        
        // Never disposed - context grows forever
        LogCtx.Logger.Info("Processing request");
    }
}
```

**✅ Solution:**
```csharp
// ✅ CORRECT: Proper context lifecycle management
public class LongRunningService
{
    public void ProcessRequest(string request)
    {
        // Each request gets a fresh, disposable context
        using var ctx = LogCtx.Set(new Props()
            .Add("RequestId", Guid.NewGuid())
            .Add("Request", request)
            .Add("Timestamp", DateTime.UtcNow));
            
        LogCtx.Logger.Info("Processing request");
        
        // Context is automatically disposed - no memory leak
    }
}
```

---

## 🔍 **Diagnostic Tools and Techniques**

### **Diagnostic 1: LogCtx Health Check**

```csharp
public static class LogCtxDiagnostics
{
    public static void PerformHealthCheck()
    {
        Console.WriteLine("=== LogCtx Health Check ===");
        
        // Check 1: Initialization
        Console.WriteLine($"LogCtx.Logger initialized: {LogCtx.Logger != null}");
        Console.WriteLine($"LogCtx.CanLog: {LogCtx.CanLog}");
        
        // Check 2: Basic functionality
        try
        {
            using var testCtx = LogCtx.Set(new Props().Add("HealthCheck", true));
            LogCtx.Logger.Info("Health check test message");
            Console.WriteLine("✅ Basic logging: PASS");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Basic logging: FAIL - {ex.Message}");
        }
        
        // Check 3: Context properties
        try
        {
            using var ctx = LogCtx.Set(new Props()
                .Add("TestString", "hello")
                .Add("TestNumber", 42)
                .Add("TestBool", true));
                
            if (ctx.ContainsKey("TestString") && 
                ctx.ContainsKey("TestNumber") && 
                ctx.ContainsKey("TestBool"))
            {
                Console.WriteLine("✅ Context properties: PASS");
            }
            else
            {
                Console.WriteLine("❌ Context properties: FAIL");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Context properties: FAIL - {ex.Message}");
        }
        
        Console.WriteLine("=== Health Check Complete ===");
    }
}
```

### **Diagnostic 2: NLog Internal Logging**

```xml
<!-- Enable detailed NLog diagnostics -->
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      internalLogLevel="Debug"
      internalLogFile="Logs/nlog-internal.log"
      internalLogToConsole="true">
  
  <!-- Your targets here -->
  
</nlog>
```

**Key Internal Log Messages to Look For:**
```
✅ GOOD: "Seq target: Successfully sent batch of X events"
❌ BAD:  "Seq target: Error sending events - Connection refused"
❌ BAD:  "Configuration file 'NLog.config' not found"
❌ BAD:  "Exception during configuration - Invalid XML"
```

### **Diagnostic 3: SEQ Connectivity Test**

```csharp
public static class SeqDiagnostics
{
    public static async Task<bool> TestSeqConnectivity(string seqUrl)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5);
            
            // Test SEQ health endpoint
            var response = await httpClient.GetAsync($"{seqUrl}/api");
            
            Console.WriteLine($"SEQ Server: {seqUrl}");
            Console.WriteLine($"Status: {response.StatusCode}");
            Console.WriteLine($"Available: {response.IsSuccessStatusCode}");
            
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SEQ Connection Failed: {ex.Message}");
            return false;
        }
    }
}
```

---

## 📋 **Common Error Messages & Solutions**

### **Error: "The name 'LogCtx' does not exist in the current context"**
**Solution:** Add shared project imports to .csproj file
**Reference:** Issue #2 above

### **Error: "Object reference not set to an instance of an object (LogCtx.Logger)"**
**Solution:** Call `FailsafeLogger.Initialize()` before using LogCtx
**Reference:** Issue #1 above

### **Error: "Current thread must be set to single thread apartment (STA)"**
**Solution:** Add `[assembly: Apartment(ApartmentState.STA)]` to test project
**Reference:** Issue #7 above

### **Error: "Configuration file 'NLog.config' not found"**
**Solution:** Place NLog.config in project root or specify custom path
**Reference:** Issue #3 above

### **Error: "Connection refused" (SEQ target)**
**Solution:** Verify SEQ server URL and test connectivity
**Reference:** Issue #4 above

### **Error: "A circular reference was detected while serializing"**
**Solution:** Avoid adding complex objects with circular references to Props
```csharp
// ❌ WRONG: Can cause circular reference
ctx.Add("ComplexObject", someObjectWithCircularRefs);

// ✅ CORRECT: Extract specific properties
ctx.Add("ObjectId", someObject.Id);
ctx.Add("ObjectName", someObject.Name);
```

---

## 🎯 **Prevention Checklist**

### **✅ Project Setup Checklist**
- [ ] Git submodule initialized and updated
- [ ] Shared projects imported correctly in .csproj
- [ ] NLog packages referenced with correct versions
- [ ] NLog.config file present and valid XML
- [ ] FailsafeLogger.Initialize() called at startup

### **✅ Configuration Checklist**
- [ ] SEQ server URL accessible from application
- [ ] API keys configured via environment variables
- [ ] Buffer sizes appropriate for application volume
- [ ] Internal logging enabled for troubleshooting
- [ ] Console target included for immediate feedback

### **✅ Testing Checklist**
- [ ] FailsafeLogger.Initialize() in [OneTimeSetUp]
- [ ] STA apartment configured for WinForms tests
- [ ] Test-specific NLog.config if needed
- [ ] Memory targets for log assertion testing
- [ ] Proper context disposal in all tests

### **✅ Performance Checklist**
- [ ] Contexts properly disposed with `using` statements
- [ ] Batch logging for high-volume operations
- [ ] Async wrappers for file/network targets
- [ ] Appropriate log levels for different environments
- [ ] Buffer sizes tuned for application patterns

---

## 🛠️ **Advanced Troubleshooting**

### **Custom Diagnostic Target**

```csharp
// Custom NLog target for diagnostics
[Target("LogCtxDiagnostic")]
public sealed class LogCtxDiagnosticTarget : TargetWithLayout
{
    protected override void Write(LogEventInfo logEvent)
    {
        var message = Layout.Render(logEvent);
        var properties = logEvent.Properties;
        
        Console.WriteLine($"[DIAGNOSTIC] {DateTime.Now:HH:mm:ss.fff}");
        Console.WriteLine($"Message: {message}");
        Console.WriteLine($"Level: {logEvent.Level}");
        Console.WriteLine($"Logger: {logEvent.LoggerName}");
        Console.WriteLine($"Properties: {properties.Count}");
        
        foreach (var prop in properties)
        {
            Console.WriteLine($"  {prop.Key} = {prop.Value}");
        }
        Console.WriteLine();
    }
}
```

### **Production Monitoring Queries**

```sql
-- SEQ queries for monitoring LogCtx health

-- Check for LogCtx initialization issues
@Message like '%LogCtx%' and @Level in ['Error', 'Fatal']

-- Monitor performance issues
Duration > 1000 and @Timestamp > DateTime.UtcNow.AddHours(-1)

-- Track error patterns
ErrorType is not null 
| group by ErrorType 
| sort by count(*) desc

-- Application health overview
Application = 'VecTool' 
| where @Timestamp > DateTime.UtcNow.AddHours(-24)
| group by @Level 
| sort by count(*) desc
```

---

**Next Steps**: See [Migration-From-Direct-NLog.md](Migration-From-Direct-NLog.md) for upgrading existing applications to LogCtx! 🚀