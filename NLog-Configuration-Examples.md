# NLog Configuration Examples for LogCtx
*Multi-environment configurations based on VecTool production patterns*

## 🎯 **Configuration Strategy Overview**

VecTool uses **environment-specific NLog configurations** with LogCtx, optimized for different deployment scenarios. This guide provides complete, tested configurations from real production usage.

### **Configuration Hierarchy**
```
Development → Staging → Production
    ↓           ↓          ↓
  Verbose    Info+      Warn+
  Console    SEQ +      SEQ +
  SEQ +      File       File +
  Debug                Email
```

---

## 🚀 **Development Environment**

### **NLog.config - Development (VecTool Pattern)**
```xml
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
      autoReload="true" 
      throwExceptions="false"
      throwConfigExceptions="true"
      internalLogLevel="Info"
      internalLogFile="Logs/nlog-internal.log">
  
  <!-- ✅ Extensions for SEQ and advanced targets -->
  <extensions>
    <add assembly="NLog.Targets.Seq" />
  </extensions>

  <targets>
    <!-- ✅ PRIMARY: SEQ Target for Structured Logging -->
    <target name="seq" 
            xsi:type="Seq" 
            serverUrl="http://localhost:5341">
      
      <!-- ✅ Application Context for Filtering -->
      <property name="Application" value="VecTool" />
      <property name="Environment" value="Development" />
      <property name="MachineName" value="${machinename}" />
      <property name="ProcessId" value="${processid}" />
      <property name="ThreadId" value="${threadid}" />
    </target>
    
    <!-- ✅ CONSOLE: Colored output for immediate feedback -->
    <target name="console" 
            xsi:type="ColoredConsole">
      <layout>${date:format=HH:mm:ss,fff} [${level:uppercase=true}] ${message}</layout>
      
      <!-- ✅ Color coding for log levels -->
      <highlight-row condition="level == LogLevel.Fatal" foregroundColor="Red" />
      <highlight-row condition="level == LogLevel.Error" foregroundColor="Red" />
      <highlight-row condition="level == LogLevel.Warn" foregroundColor="Yellow" />
      <highlight-row condition="level == LogLevel.Info" foregroundColor="Cyan" />
      <highlight-row condition="level == LogLevel.Debug" foregroundColor="Green" />
    </target>
    
    <!-- ✅ DEBUG: Visual Studio debug output -->
    <target name="debug" 
            xsi:type="Debugger"
            layout="${threadid} ${time} [${level:uppercase=true}] ${logger:shortName=true} - ${message} ${exception:format=tostring}" />
  </targets>

  <rules>
    <!-- ✅ All logs to SEQ for analysis -->
    <logger name="*" minlevel="Trace" writeTo="seq" />
    <!-- ✅ Debug+ to Visual Studio output -->
    <logger name="*" minlevel="Debug" writeTo="debug" />
    <!-- ✅ Info+ to console for visibility -->
    <logger name="*" minlevel="Info" writeTo="console" />
  </rules>

</nlog>
```

### **Development Usage Pattern**
```csharp
using LogCtxShared;
using NLogShared;

class Program 
{
    static void Main(string[] args)
    {
        // ✅ Development: Failsafe initialization with verbose logging
        FailsafeLogger.Initialize("NLog.config");
        
        using var ctx = LogCtx.Set(new Props()
            .Add("Application", "VecTool")
            .Add("Environment", "Development")
            .Add("Version", "4.25.1007"));
            
        LogCtx.Logger.Info("Application started in development mode");
        
        // Your application logic...
    }
}
```

---

## 🏢 **Staging Environment** 

### **NLog.Staging.config - Staging (Production-like)**
```xml
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
      autoReload="true" 
      throwExceptions="false"
      internalLogLevel="Warn"
      internalLogFile="Logs/nlog-internal.log">
  
  <extensions>
    <add assembly="NLog.Targets.Seq" />
  </extensions>

  <targets>
    <!-- ✅ SEQ: Buffered for Performance -->
    <target name="seq" 
            xsi:type="BufferingWrapper"
            bufferSize="1000"
            flushTimeout="2000"
            slidingTimeout="false">
      
      <target xsi:type="Seq" 
              serverUrl="https://seq-staging.company.com"
              apiKey="${environment:SEQ_STAGING_API_KEY}">
        
        <!-- ✅ Staging Environment Properties -->
        <property name="Application" value="VecTool" />
        <property name="Environment" value="Staging" />
        <property name="MachineName" value="${machinename}" />
        <property name="ProcessId" value="${processid}" />
        <property name="DeploymentId" value="${environment:DEPLOYMENT_ID}" />
        <property name="BuildNumber" value="${environment:BUILD_NUMBER}" />
      </target>
    </target>
    
    <!-- ✅ FILE: Rolling logs for debugging -->
    <target name="file" 
            xsi:type="AutoFlushWrapper">
      <target xsi:type="File" 
              name="file"
              fileName="Logs/VecTool-${shortdate}.log"
              layout="${longdate} [${level:uppercase=true}] ${logger} - ${message} ${exception:format=tostring}"
              archiveFileName="Logs/VecTool-${shortdate}.{##}.log"
              archiveAboveSize="50000000"
              archiveNumbering="Sequence"
              maxArchiveFiles="10"
              concurrentWrites="false" />
    </target>
    
    <!-- ✅ CONSOLE: Minimal for service visibility -->
    <target name="console" 
            xsi:type="Console"
            layout="${time} [${level}] ${message}" />
  </targets>

  <rules>
    <!-- ✅ Info+ to SEQ for monitoring -->
    <logger name="*" minlevel="Info" writeTo="seq" />
    <!-- ✅ Error+ to files for troubleshooting -->
    <logger name="*" minlevel="Error" writeTo="file" />
    <!-- ✅ Warn+ to console for immediate visibility -->
    <logger name="*" minlevel="Warn" writeTo="console" />
  </rules>

</nlog>
```

### **Staging Environment Variables**
```bash
# Staging environment configuration
export SEQ_STAGING_API_KEY="your-staging-seq-api-key"
export DEPLOYMENT_ID="staging-v4.25.1007"
export BUILD_NUMBER="1007"
export ASPNETCORE_ENVIRONMENT="Staging"
```

---

## 🏭 **Production Environment**

### **NLog.Production.config - High Performance + Reliability**
```xml
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
      autoReload="true" 
      throwExceptions="false"
      internalLogLevel="Error"
      internalLogFile="Logs/nlog-internal.log">
  
  <extensions>
    <add assembly="NLog.Targets.Seq" />
  </extensions>

  <targets>
    <!-- ✅ SEQ: High-Performance Buffered -->
    <target name="seq" 
            xsi:type="AsyncWrapper"
            timeToSleepBetweenBatches="50"
            batchSize="5000"
            queueLimit="200000"
            overflowAction="Discard">
      
      <target name="seqBuffer" 
              xsi:type="BufferingWrapper"
              bufferSize="5000"
              flushTimeout="1000">
        
        <target xsi:type="Seq" 
                serverUrl="https://seq.company.com"
                apiKey="${environment:SEQ_PROD_API_KEY}">
          
          <!-- ✅ Production Context -->
          <property name="Application" value="VecTool" />
          <property name="Environment" value="Production" />
          <property name="MachineName" value="${machinename}" />
          <property name="ProcessId" value="${processid}" />
          <property name="ServerRole" value="${environment:SERVER_ROLE}" />
          <property name="DataCenter" value="${environment:DATA_CENTER}" />
          <property name="Version" value="${assembly-version}" />
        </target>
      </target>
    </target>
    
    <!-- ✅ FILE: Critical error logging -->
    <target name="errorFile" 
            xsi:type="AsyncWrapper"
            timeToSleepBetweenBatches="50"
            batchSize="1000"
            queueLimit="10000"
            overflowAction="Block">
      
      <target name="errorFileTarget" 
              xsi:type="AutoFlushWrapper">
        <target xsi:type="File" 
                name="file"
                fileName="Logs/VecTool-Error-${shortdate}.log"
                layout="${longdate} [${level:uppercase=true}] ${logger} - ${message} ${exception:format=tostring}"
                archiveFileName="Logs/VecTool-Error-${shortdate}.{##}.log"
                archiveAboveSize="100000000"
                archiveNumbering="Sequence"
                maxArchiveFiles="50"
                concurrentWrites="false" />
      </target>
    </target>
    
    <!-- ✅ EMAIL: Critical alerts -->
    <target name="email" 
            xsi:type="Mail"
            smtpServer="${environment:SMTP_SERVER}"
            smtpPort="${environment:SMTP_PORT}"
            smtpAuthentication="Basic"
            smtpUserName="${environment:SMTP_USER}"
            smtpPassword="${environment:SMTP_PASS}"
            enableSsl="true"
            from="noreply@company.com"
            to="${environment:ALERT_EMAIL}"
            subject="VecTool Critical Error - ${machinename}"
            body="${longdate} [${level}] ${message}${newline}${exception:format=tostring}"
            layout="${longdate} [${level}] ${logger}: ${message} ${exception:format=tostring}" />
  </targets>

  <rules>
    <!-- ✅ Warn+ to SEQ for monitoring -->
    <logger name="*" minlevel="Warn" writeTo="seq" />
    <!-- ✅ Error+ to file for persistence -->
    <logger name="*" minlevel="Error" writeTo="errorFile" />
    <!-- ✅ Fatal+ to email for immediate alerts -->
    <logger name="*" minlevel="Fatal" writeTo="email" />
  </rules>

</nlog>
```

### **Production Environment Variables**
```bash
# Production environment configuration
export SEQ_PROD_API_KEY="your-production-seq-api-key"
export SERVER_ROLE="api-server"
export DATA_CENTER="east-us-1"
export ASPNETCORE_ENVIRONMENT="Production"
export SMTP_SERVER="smtp.company.com"
export SMTP_PORT="587"
export SMTP_USER="alerts@company.com"
export SMTP_PASS="your-smtp-password"
export ALERT_EMAIL="ops-team@company.com"
```

---

## 🧪 **Testing Environment**

### **NLog.Test.config - Unit & Integration Tests**
```xml
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
      autoReload="true" 
      throwExceptions="false"
      internalLogLevel="Off">
  
  <targets>
    <!-- ✅ CONSOLE: Test execution visibility -->
    <target name="testConsole" 
            xsi:type="Console"
            layout="[${level:uppercase=true}] ${logger:shortName=true}: ${message}" />
    
    <!-- ✅ DEBUG: Visual Studio test output -->
    <target name="testDebug" 
            xsi:type="Debugger"
            layout="${time} [${level}] ${message} ${exception:format=tostring}" />
    
    <!-- ✅ MEMORY: In-memory for test assertions -->
    <target name="memory" 
            xsi:type="Memory" 
            name="memoryTarget"
            layout="${level}|${logger}|${message}" />
  </targets>

  <rules>
    <!-- ✅ Error+ to console for test failures -->
    <logger name="*" minlevel="Error" writeTo="testConsole" />
    <!-- ✅ Debug+ to Visual Studio output -->
    <logger name="*" minlevel="Debug" writeTo="testDebug" />
    <!-- ✅ All to memory for test verification -->
    <logger name="*" minlevel="Trace" writeTo="memory" />
  </rules>

</nlog>
```

### **Testing Usage Pattern**
```csharp
using NUnit.Framework;
using Shouldly;
using LogCtxShared;
using NLogShared;

[TestFixture]
public class LogCtxTests
{
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        // ✅ Initialize with test configuration
        FailsafeLogger.Initialize("NLog.Test.config");
    }
    
    [Test]
    public void LogCtx_ShouldCaptureSourceLocation()
    {
        // Arrange
        using var ctx = LogCtx.Set(new Props()
            .Add("TestMethod", nameof(LogCtx_ShouldCaptureSourceLocation))
            .Add("TestCategory", "SourceLocation"));
            
        // Act
        LogCtx.Logger.Info("Test log message");
        
        // Assert - Verify properties were captured
        ctx.ContainsKey(LogCtx.FILE).ShouldBeTrue();
        ctx.ContainsKey(LogCtx.METHOD).ShouldBeTrue();
        ctx.ContainsKey(LogCtx.LINE).ShouldBeTrue();
    }
}
```

---

## ⚙️ **Configuration Management Patterns**

### **Environment-Specific Config Selection**
```csharp
using LogCtxShared;
using NLogShared;

public static class LoggingBootstrap 
{
    public static void Initialize()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        
        var configFiles = new[]
        {
            $"NLog.{environment}.config",  // Environment-specific
            "NLog.config",                 // Default
            null                           // Fallback to minimal config
        };
        
        foreach (var configFile in configFiles)
        {
            if (configFile == null)
            {
                // Use FailsafeLogger for fallback
                FailsafeLogger.Initialize();
                return;
            }
            
            if (File.Exists(configFile))
            {
                FailsafeLogger.Initialize(configFile);
                return;
            }
        }
    }
}
```

### **Docker Configuration Pattern**
```dockerfile
# Dockerfile for VecTool
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# Copy environment-specific configs
COPY Config/NLog.Production.config ./NLog.config
COPY Config/NLog.Staging.config ./NLog.Staging.config

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore VecTool.sln
RUN dotnet build VecTool.sln -c Release

FROM build AS publish
RUN dotnet publish VecTool.UI/VecTool.UI.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Environment determines which config is used
ENV ASPNETCORE_ENVIRONMENT=Production
ENV SEQ_PROD_API_KEY=your-api-key

ENTRYPOINT ["dotnet", "VecTool.UI.dll"]
```

### **Kubernetes ConfigMap Pattern**
```yaml
# k8s-nlog-config.yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: vectool-nlog-config
data:
  NLog.config: |
    <?xml version="1.0" encoding="utf-8" ?>
    <nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd">
      <extensions>
        <add assembly="NLog.Targets.Seq" />
      </extensions>
      <targets>
        <target name="seq" 
                xsi:type="Seq" 
                serverUrl="http://seq-service:5341"
                apiKey="${environment:SEQ_API_KEY}">
          <property name="Application" value="VecTool" />
          <property name="Environment" value="Kubernetes" />
          <property name="PodName" value="${environment:POD_NAME}" />
          <property name="NodeName" value="${environment:NODE_NAME}" />
        </target>
      </targets>
      <rules>
        <logger name="*" minlevel="Info" writeTo="seq" />
      </rules>
    </nlog>
```

---

## 🔧 **Advanced Target Configurations**

### **Rolling File with Size and Time-Based Archiving**
```xml
<target name="rollingFile" 
        xsi:type="File"
        fileName="Logs/VecTool-${shortdate}.log"
        layout="${longdate} [${level}] ${logger}: ${message} ${exception:format=tostring}"
        
        <!-- ✅ Time-based rolling -->
        archiveFileName="Logs/Archive/VecTool-{#}.log"
        archiveEvery="Day"
        archiveOldFileOnStartup="true"
        
        <!-- ✅ Size-based rolling -->
        archiveAboveSize="100000000"
        archiveNumbering="Rolling"
        maxArchiveFiles="30"
        
        <!-- ✅ Performance settings -->
        concurrentWrites="false"
        keepFileOpen="true"
        openFileCacheTimeout="30" />
```

### **SEQ with Custom Properties and Filtering**
```xml
<target name="seqAdvanced" 
        xsi:type="Seq" 
        serverUrl="http://localhost:5341"
        apiKey="${environment:SEQ_API_KEY}">
  
  <!-- ✅ Application-specific properties -->
  <property name="Application" value="VecTool" />
  <property name="Component" value="${logger:shortName=true}" />
  <property name="Environment" value="${environment:ASPNETCORE_ENVIRONMENT:default=Development}" />
  <property name="Version" value="${assembly-version}" />
  
  <!-- ✅ System properties -->
  <property name="MachineName" value="${machinename}" />
  <property name="ProcessId" value="${processid}" />
  <property name="ThreadId" value="${threadid:asNumber}" />
  
  <!-- ✅ Performance properties -->
  <property name="ElapsedMilliseconds" value="${scopeproperty:ElapsedMilliseconds}" />
  <property name="OperationType" value="${scopeproperty:OperationType}" />
  
  <!-- ✅ User context -->
  <property name="UserId" value="${scopeproperty:UserId}" />
  <property name="SessionId" value="${scopeproperty:SessionId}" />
</target>
```

### **Email Alerts with HTML Formatting**
```xml
<target name="emailAlerts" 
        xsi:type="Mail"
        smtpServer="smtp.company.com"
        smtpPort="587"
        smtpAuthentication="Basic"
        smtpUserName="${environment:SMTP_USER}"
        smtpPassword="${environment:SMTP_PASS}"
        enableSsl="true"
        from="vectool-alerts@company.com"
        to="dev-team@company.com"
        cc="ops-team@company.com"
        subject="🚨 VecTool Alert - ${level} on ${machinename}"
        html="true"
        body="&lt;html&gt;&lt;body&gt;
              &lt;h2&gt;VecTool Alert&lt;/h2&gt;
              &lt;p&gt;&lt;strong&gt;Time:&lt;/strong&gt; ${longdate}&lt;/p&gt;
              &lt;p&gt;&lt;strong&gt;Level:&lt;/strong&gt; ${level}&lt;/p&gt;
              &lt;p&gt;&lt;strong&gt;Logger:&lt;/strong&gt; ${logger}&lt;/p&gt;
              &lt;p&gt;&lt;strong&gt;Message:&lt;/strong&gt; ${message}&lt;/p&gt;
              &lt;p&gt;&lt;strong&gt;Machine:&lt;/strong&gt; ${machinename}&lt;/p&gt;
              &lt;p&gt;&lt;strong&gt;Exception:&lt;/strong&gt;&lt;/p&gt;
              &lt;pre&gt;${exception:format=tostring}&lt;/pre&gt;
              &lt;/body&gt;&lt;/html&gt;" />
```

---

## 🎯 **Best Practices Summary**

### **✅ Environment Configuration Strategy**
1. **Development**: Verbose logging to SEQ + Console + Debug output
2. **Staging**: Info+ to SEQ, Error+ to files, performance buffering
3. **Production**: Warn+ to SEQ, Error+ to files, Fatal+ to email alerts
4. **Testing**: Error+ to console, All to memory for assertions

### **✅ Performance Optimization**
- **Use BufferingWrapper** for high-volume SEQ logging
- **Use AsyncWrapper** for file logging in production
- **Configure appropriate buffer sizes** based on log volume
- **Set overflow policies** to prevent memory issues

### **✅ Reliability Patterns**
- **Never throwExceptions** in production configurations
- **Use AutoFlushWrapper** for critical error logs
- **Configure multiple targets** for redundancy
- **Set internal logging** to catch NLog issues

### **✅ SEQ Integration**
- **Include structured properties** for powerful querying
- **Use environment variables** for API keys and URLs
- **Configure application context** for filtering
- **Buffer appropriately** for performance

### **✅ Security Considerations**
- **Use environment variables** for sensitive data (API keys, SMTP passwords)
- **Don't log sensitive information** (passwords, tokens, PII)
- **Secure log file permissions** in production
- **Use HTTPS** for SEQ connections in production

---

## 🔗 **Related Documentation**

- **[SEQ-Configuration-Guide.md](SEQ-Configuration-Guide.md)** - Complete SEQ setup and queries
- **[Usage-Patterns-Examples.md](Usage-Patterns-Examples.md)** - Real VecTool usage patterns  
- **[API-Complete-Reference.md](API-Complete-Reference.md)** - Complete LogCtx API documentation
- **[Troubleshooting.md](Troubleshooting.md)** - Common NLog configuration issues

**Next Steps**: See [Best-Practices.md](Best-Practices.md) for LogCtx coding standards and patterns! 🚀