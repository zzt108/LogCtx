# SEQ Configuration Guide for LogCtx
*Complete setup guide for SEQ structured logging with LogCtx and NLog*

## 🎯 **Quick Start - Get SEQ Running in 5 Minutes**

### **Step 1: Install & Start SEQ Server**

```bash
# Option 1: Docker (Recommended)
docker run --name seq -d -p 5341:5341 -p 80:80 datalust/seq:latest

# Option 2: Download SEQ installer
# Download from: https://getseq.net/Download
# Install and run as Windows service

# Verify SEQ is running
# Navigate to: http://localhost:5341
```

### **Step 2: Configure NLog.config for SEQ**

**Basic SEQ Configuration (based on VecTool pattern):**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
      autoReload="true" 
      throwExceptions="false">
  
  <extensions>
    <add assembly="NLog.Targets.Seq" />
  </extensions>

  <targets>
    <!-- ✅ Primary SEQ Target - Structured Logging -->
    <target name="seq" 
            xsi:type="Seq" 
            serverUrl="http://localhost:5341">
      
      <!-- ✅ Application Properties for Filtering -->
      <property name="Application" value="VecTool" />
      <property name="Environment" value="Development" />
      <property name="MachineName" value="${machinename}" />
      <property name="ProcessId" value="${processid}" />
      <property name="ThreadId" value="${threadid}" />
      
    </target>
    
    <!-- ✅ Console Fallback -->
    <target name="console" 
            xsi:type="ColoredConsole">
      <layout>${date:format=HH:mm:ss,fff} [${level:uppercase=true}] ${logger}: ${message} ${exception:format=tostring}</layout>
      
      <highlight-row condition="level == LogLevel.Fatal" foregroundColor="Red" />
      <highlight-row condition="level == LogLevel.Error" foregroundColor="Red" />
      <highlight-row condition="level == LogLevel.Warn" foregroundColor="Yellow" />
      <highlight-row condition="level == LogLevel.Info" foregroundColor="Cyan" />
      <highlight-row condition="level == LogLevel.Debug" foregroundColor="Green" />
    </target>
  </targets>

  <rules>
    <!-- ✅ All logs to SEQ for structured analysis -->
    <logger name="*" minlevel="Debug" writeTo="seq" />
    <!-- ✅ Important logs to console for immediate feedback -->
    <logger name="*" minlevel="Info" writeTo="console" />
  </rules>

</nlog>
```

### **Step 3: Initialize LogCtx with SEQ**

```csharp
using LogCtxShared;
using NLogShared;

class Program
{
    static async Task Main(string[] args)
    {
        // ✅ Initialize LogCtx once at application startup
        FailsafeLogger.Initialize("NLog.config");
        
        // ✅ Application startup context with SEQ-friendly properties
        using var startupCtx = LogCtx.Set(new Props()
            .Add("Application", "VecTool")
            .Add("Version", "4.0.3")
            .Add("Environment", "Development")
            .Add("StartupTime", DateTime.UtcNow));
            
        LogCtx.Logger.Info("Application startup complete");
        
        // Your application code...
        await ProcessDataAsync();
    }
}
```

## 🎪 **Advanced SEQ Configuration**

### **Production-Ready Configuration**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
      autoReload="true" 
      throwExceptions="false"
      internalLogLevel="Info"
      internalLogFile="Logs/nlog-internal.log">
  
  <extensions>
    <add assembly="NLog.Targets.Seq" />
  </extensions>

  <targets>
    <!-- ✅ Buffered SEQ Target for High Performance -->
    <target name="seq" 
            xsi:type="BufferingWrapper"
            bufferSize="1000"
            flushTimeout="2000">
      
      <target xsi:type="Seq" 
              serverUrl="http://localhost:5341"
              apiKey="${environment:SEQ_API_KEY}">
        
        <!-- ✅ Rich Application Context -->
        <property name="Application" value="VecTool" />
        <property name="Environment" value="${environment:ASPNETCORE_ENVIRONMENT:default=Production}" />
        <property name="MachineName" value="${machinename}" />
        <property name="ProcessId" value="${processid}" />
        <property name="ThreadId" value="${threadid:asNumber}" />
        <property name="AssemblyVersion" value="${assembly-version}" />
        <property name="GitCommit" value="${environment:GIT_COMMIT}" />
        
      </target>
    </target>
    
    <!-- ✅ File Logging for SEQ Downtime -->
    <target name="file" 
            xsi:type="File"
            fileName="Logs/${shortdate}.log"
            layout="${longdate} [${level:uppercase=true}] ${logger} - ${message} ${exception:format=tostring}"
            archiveAboveSize="50000000"
            archiveNumbering="Sequence"
            concurrentWrites="false" />
    
    <!-- ✅ Console for Development -->
    <target name="console" 
            xsi:type="ColoredConsole">
      <layout>${date:format=HH:mm:ss,fff} [${level:uppercase=true}] ${message}</layout>
      <highlight-row condition="level == LogLevel.Error" foregroundColor="Red" />
      <highlight-row condition="level == LogLevel.Warn" foregroundColor="Yellow" />
    </target>
  </targets>

  <rules>
    <!-- ✅ All structured logs to SEQ -->
    <logger name="*" minlevel="Trace" writeTo="seq" />
    <!-- ✅ Backup to file for critical logs -->
    <logger name="*" minlevel="Error" writeTo="file" />
    <!-- ✅ Console for development -->
    <logger name="*" minlevel="Info" writeTo="console" />
  </rules>

</nlog>
```

### **Multi-Environment Configuration**

```xml
<!-- Development Environment -->
<target name="seq-dev" 
        xsi:type="Seq" 
        serverUrl="http://localhost:5341">
  <property name="Environment" value="Development" />
</target>

<!-- Staging Environment -->
<target name="seq-staging" 
        xsi:type="Seq" 
        serverUrl="https://seq-staging.company.com"
        apiKey="${environment:SEQ_STAGING_API_KEY}">
  <property name="Environment" value="Staging" />
</target>

<!-- Production Environment -->
<target name="seq-prod" 
        xsi:type="Seq" 
        serverUrl="https://seq.company.com"
        apiKey="${environment:SEQ_PROD_API_KEY}">
  <property name="Environment" value="Production" />
</target>
```

## 🔍 **LogCtx + SEQ Structured Logging Patterns**

### **Pattern 1: Operation Tracking**

```csharp
public async Task ProcessFileAsync(string filePath)
{
    // ✅ SEQ-optimized context properties
    using var ctx = LogCtx.Set(new Props()
        .Add("Operation", "ProcessFile")
        .Add("FilePath", filePath)
        .Add("OperationId", Guid.NewGuid())
        .Add("UserId", GetCurrentUserId())
        .Add("StartTime", DateTime.UtcNow));
    
    LogCtx.Logger.Info("File processing started");
    
    var stopwatch = Stopwatch.StartNew();
    
    try
    {
        var fileInfo = new FileInfo(filePath);
        ctx.Add("FileSize", fileInfo.Length);
        ctx.Add("FileExtension", fileInfo.Extension);
        
        var content = await File.ReadAllTextAsync(filePath);
        
        // Process content...
        
        stopwatch.Stop();
        ctx.Add("Duration", stopwatch.ElapsedMilliseconds);
        ctx.Add("Success", true);
        
        LogCtx.Logger.Info("File processing completed successfully");
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        ctx.Add("Duration", stopwatch.ElapsedMilliseconds);
        ctx.Add("Success", false);
        ctx.Add("ErrorType", ex.GetType().Name);
        
        LogCtx.Logger.Error(ex, "File processing failed");
        throw;
    }
}
```

### **Pattern 2: User Action Tracking**

```csharp
public async Task<bool> ExecuteUserActionAsync(string action, object parameters)
{
    using var ctx = LogCtx.Set(new Props()
        .Add("UserAction", action)
        .Add("Parameters", parameters)
        .Add("SessionId", GetSessionId())
        .Add("UserRole", GetUserRole())
        .Add("ClientIP", GetClientIP())
        .Add("Timestamp", DateTime.UtcNow));
    
    LogCtx.Logger.Info("User action initiated");
    
    var result = await ExecuteActionAsync(action, parameters);
    
    ctx.Add("ActionResult", result ? "Success" : "Failed");
    ctx.Add("ResponseTime", GetResponseTime());
    
    LogCtx.Logger.Info("User action completed");
    
    return result;
}
```

### **Pattern 3: Performance Monitoring**

```csharp
public async Task<List<T>> ProcessBatchAsync<T>(List<T> items, string batchId)
{
    using var batchCtx = LogCtx.Set(new Props()
        .Add("BatchId", batchId)
        .Add("BatchSize", items.Count)
        .Add("ItemType", typeof(T).Name)
        .Add("ProcessingStarted", DateTime.UtcNow));
    
    LogCtx.Logger.Info("Batch processing started");
    
    var results = new List<T>();
    var stopwatch = Stopwatch.StartNew();
    var processedCount = 0;
    var failedCount = 0;
    
    foreach (var item in items)
    {
        using var itemCtx = LogCtx.Set(new Props()
            .Add("BatchId", batchId)
            .Add("ItemIndex", processedCount)
            .Add("ItemId", GetItemId(item)));
        
        try
        {
            var result = await ProcessSingleItemAsync(item);
            results.Add(result);
            processedCount++;
            
            // Log every 100 items for large batches
            if (processedCount % 100 == 0)
            {
                LogCtx.Logger.Debug("Batch progress update");
            }
        }
        catch (Exception ex)
        {
            failedCount++;
            LogCtx.Logger.Error(ex, "Item processing failed");
        }
    }
    
    stopwatch.Stop();
    
    batchCtx.Add("ProcessedCount", processedCount);
    batchCtx.Add("FailedCount", failedCount);
    batchCtx.Add("SuccessRate", (double)processedCount / items.Count);
    batchCtx.Add("TotalDuration", stopwatch.ElapsedMilliseconds);
    batchCtx.Add("AverageItemDuration", stopwatch.ElapsedMilliseconds / items.Count);
    batchCtx.Add("ProcessingCompleted", DateTime.UtcNow);
    
    LogCtx.Logger.Info("Batch processing completed");
    
    return results;
}
```

## 🎯 **SEQ Dashboard & Query Examples**

### **Essential SEQ Queries**

#### **1. Application Overview**
```sql
-- All logs from VecTool application
Application = 'VecTool'

-- Logs from specific environment
Application = 'VecTool' and Environment = 'Production'
```

#### **2. Performance Analysis**
```sql
-- Operations taking longer than 1 second
Duration > 1000

-- Average operation duration by operation type
select avg(Duration), Operation 
from stream 
group by Operation 
order by avg(Duration) desc

-- Slow file processing operations
Operation = 'ProcessFile' and Duration > 5000
```

#### **3. Error Analysis**
```sql
-- All errors in the last hour
@Level = 'Error' and @Timestamp > DateTime.UtcNow.AddHours(-1)

-- Errors by type
select count(*), ErrorType 
from stream 
where ErrorType is not null 
group by ErrorType 
order by count(*) desc

-- Failed user actions
UserAction is not null and ActionResult = 'Failed'
```

#### **4. User Activity**
```sql
-- User actions by session
select count(*), SessionId, UserRole 
from stream 
where UserAction is not null 
group by SessionId, UserRole 
order by count(*) desc

-- Most common user actions
select count(*), UserAction 
from stream 
where UserAction is not null 
group by UserAction 
order by count(*) desc
```

#### **5. System Health**
```sql
-- Recent application startups
@Message like '%startup%' and @Timestamp > DateTime.UtcNow.AddHours(-24)

-- High memory usage alerts
MemoryUsage > 80

-- Thread performance issues
ThreadId is not null | group by ThreadId | sort by count desc
```

### **SEQ Signals (Alerts) Configuration**

Based on VecTool patterns, create these alerts:

#### **1. High Error Rate Signal**
```sql
-- Trigger: More than 10 errors in 5 minutes
@Level in ['Error', 'Fatal'] 
| group by bin(@Timestamp, 5m) 
| where count(*) > 10
```

#### **2. Slow Operations Signal**
```sql
-- Trigger: Operations taking longer than 10 seconds
Duration > 10000
```

#### **3. Application Startup Signal**
```sql
-- Trigger: Application restarts (for monitoring)
@Message like '%startup%'
```

#### **4. Failed Batch Processing Signal**
```sql
-- Trigger: Batch processing with >20% failure rate
SuccessRate < 0.8 and BatchSize > 10
```

## 🚀 **SEQ Best Practices for LogCtx**

### **✅ Property Naming Conventions**

```csharp
// ✅ GOOD: PascalCase properties for SEQ queries
ctx.Add("UserId", userId);
ctx.Add("OperationId", operationId);
ctx.Add("Duration", elapsedMs);
ctx.Add("Success", true);

// ❌ BAD: Inconsistent naming makes querying difficult
ctx.Add("user_id", userId);
ctx.Add("operationID", operationId);
ctx.Add("duration_ms", elapsedMs);
```

### **✅ Structured Data Types**

```csharp
// ✅ GOOD: Proper data types for SEQ analysis
ctx.Add("Duration", 1234);           // Numeric for mathematical operations
ctx.Add("Timestamp", DateTime.UtcNow); // DateTime for time-based queries
ctx.Add("Success", true);            // Boolean for filtering

// ❌ BAD: Everything as strings loses SEQ's power
ctx.Add("Duration", "1234ms");       // Can't do mathematical operations
ctx.Add("Timestamp", "2025-10-07");  // Can't do time-based filtering
ctx.Add("Success", "true");          // Can't do boolean operations
```

### **✅ Context Hierarchy**

```csharp
// ✅ GOOD: Hierarchical context for drill-down analysis
using var sessionCtx = LogCtx.Set(new Props()
    .Add("SessionId", sessionId)
    .Add("UserId", userId)
    .Add("UserRole", userRole));

using var operationCtx = LogCtx.Set(new Props()
    .Add("SessionId", sessionId)     // Inherited for correlation
    .Add("Operation", "FileUpload")
    .Add("OperationId", Guid.NewGuid()));

using var fileCtx = LogCtx.Set(new Props()
    .Add("SessionId", sessionId)     // Inherited for correlation
    .Add("OperationId", operationId) // Inherited for correlation  
    .Add("FileName", fileName)
    .Add("FileSize", fileSize));
```

### **✅ Performance Considerations**

```csharp
// ✅ GOOD: Batch properties for performance
var props = new Props()
    .Add("UserId", userId)
    .Add("SessionId", sessionId)
    .Add("Operation", operation)
    .Add("Timestamp", DateTime.UtcNow);

using var ctx = LogCtx.Set(props);

// ❌ BAD: Multiple context creations in tight loops
for (int i = 0; i < 1000; i++)
{
    using var ctx = LogCtx.Set(new Props().Add("Index", i)); // Too much overhead
    ProcessItem(i);
}
```

## 🔧 **Environment-Specific Configuration**

### **Development Environment**

```xml
<!-- Development: Verbose logging to SEQ + Console -->
<target name="seq" xsi:type="Seq" serverUrl="http://localhost:5341">
  <property name="Environment" value="Development" />
</target>

<rules>
  <logger name="*" minlevel="Trace" writeTo="seq" />
  <logger name="*" minlevel="Debug" writeTo="console" />
</rules>
```

### **Staging Environment**

```xml
<!-- Staging: Info+ to SEQ, Error+ to file -->
<target name="seq" xsi:type="Seq" 
        serverUrl="https://seq-staging.company.com"
        apiKey="${environment:SEQ_STAGING_API_KEY}">
  <property name="Environment" value="Staging" />
</target>

<rules>
  <logger name="*" minlevel="Info" writeTo="seq" />
  <logger name="*" minlevel="Error" writeTo="file" />
</rules>
```

### **Production Environment**

```xml
<!-- Production: Warn+ to SEQ with buffering -->
<target name="seq" 
        xsi:type="BufferingWrapper"
        bufferSize="5000"
        flushTimeout="1000">
  <target xsi:type="Seq" 
          serverUrl="https://seq.company.com"
          apiKey="${environment:SEQ_PROD_API_KEY}">
    <property name="Environment" value="Production" />
  </target>
</target>

<rules>
  <logger name="*" minlevel="Warn" writeTo="seq" />
  <logger name="*" minlevel="Fatal" writeTo="email" />
</rules>
```

## 🎯 **Testing SEQ Integration**

### **Verification Steps**

1. **Check SEQ Connectivity**:
   ```csharp
   using var testCtx = LogCtx.Set(new Props()
       .Add("Test", "SEQ Connectivity")
       .Add("Timestamp", DateTime.UtcNow));
   
   LogCtx.Logger.Info("SEQ connectivity test");
   ```

2. **Verify Structured Properties**:
   - Navigate to SEQ dashboard (http://localhost:5341)
   - Look for your test log entry
   - Click to expand and verify properties are structured (not just text)

3. **Test Query Performance**:
   ```sql
   -- Should return results quickly if indexing is working
   Test = 'SEQ Connectivity'
   ```

### **Common Troubleshooting**

#### **SEQ Server Not Receiving Logs**
1. Check NLog internal logs: `internalLogFile="Logs/nlog-internal.log"`
2. Verify SEQ server URL is accessible
3. Check API key if using authentication
4. Ensure NLog.Targets.Seq package is referenced

#### **Properties Not Structured**
1. Verify `compactMode="true"` is NOT set if you want structured properties
2. Don't use string interpolation in log messages
3. Use LogCtx property system, not NLog's `{PropertyName}` syntax

#### **Performance Issues**
1. Use `BufferingWrapper` for high-volume applications
2. Adjust `bufferSize` and `flushTimeout` based on your needs
3. Consider increasing SEQ's ingestion limits
4. Use appropriate log levels for different environments

---

## 🎯 **Key Takeaways**

1. **SEQ + LogCtx = Powerful Debugging**: Structured properties make finding issues trivial
2. **Property Naming Matters**: Use PascalCase and meaningful names for better queries  
3. **Context Hierarchy**: Build logical property relationships for drill-down analysis
4. **Environment-Appropriate Logging**: Verbose in dev, warn+ in production
5. **Performance Considerations**: Use buffering and appropriate batch sizes

**Next Steps**: See [Usage-Patterns-Examples.md](Usage-Patterns-Examples.md) for real-world LogCtx + SEQ patterns! 🚀