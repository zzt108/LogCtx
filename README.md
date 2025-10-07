# LogCtx - Structured Logging Library

**Battle-tested structured logging for .NET applications with zero-config initialization and automatic context capture.**

LogCtx is a production-ready logging library that provides structured logging capabilities with automatic source location capture, fluent property building, and seamless SEQ integration. It's designed to never throw exceptions during initialization or logging operations.

---

## 🚀 **Quick Start**

### **1. Installation & Setup**

⚠️ **MANDATORY INITIALIZATION** - Without this, LogCtx.Logger will be null and crash your application!

```csharp
using NLogShared;   // Required for FailsafeLogger
using LogCtxShared; // Required for LogCtx classes
using NLog;

// Initialize robust, non-throwing logging (MUST be called first!)
FailsafeLogger.Initialize("NLog.config");
```

### **2. Basic Usage**

```csharp
using NLogShared;   // Required for FailsafeLogger
using LogCtxShared; // Required for LogCtx classes

// Simple logging with automatic context
using var ctx = LogCtx.Set(); // ✅ Captures file/line automatically
ctx.AddProperty("UserId", 12345);
ctx.AddProperty("Operation", "UserLogin");
LogCtx.Logger.Information("User logged in successfully", ctx);
```

### **3. Exception Handling**

```csharp
using NLogShared;   // Required for FailsafeLogger
using LogCtxShared; // Required for LogCtx classes

try
{
    // Your application logic
    ProcessUserData(userData);
}
catch (Exception ex)
{
    using var errorCtx = LogCtx.Set();
    errorCtx.AddProperty("UserId", userData.Id);
    errorCtx.AddProperty("Operation", "ProcessUserData");
    errorCtx.AddProperty("ErrorType", ex.GetType().Name);
    LogCtx.Logger.Error("User data processing failed", ex, errorCtx);
    throw;
}
```

---

## 🎯 **Key Features**

### **Zero-Config Initialization**
- ✅ **Never throws exceptions** during initialization
- ✅ **Graceful fallback** when configuration is missing
- ✅ **Thread-safe** initialization

### **Automatic Context Capture**
- ✅ **File and line number** captured automatically
- ✅ **Method name** and **class name** resolution
- ✅ **Timestamp** and **thread ID** tracking

### **Fluent Property Building**
- ✅ **Method chaining** for readable code
- ✅ **Type-safe** property addition
- ✅ **Structured data** optimization for SEQ queries

### **Production-Ready**
- ✅ **Battle-tested** in enterprise applications
- ✅ **High-performance** with minimal overhead
- ✅ **Memory efficient** context management

---

## 📋 **Complete Application Example**

```csharp
// ✅ COMPLETE APPLICATION SETUP
using NLogShared;   // Required for FailsafeLogger
using LogCtxShared; // Required for LogCtx classes
using NLog;

namespace YourApplication
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            // ⚠️ MANDATORY - Initialize logging first!
            FailsafeLogger.Initialize("NLog.config");
            
            // Application startup context
            using var startupCtx = LogCtx.Set();
            startupCtx.AddProperty("ApplicationName", "YourApp");
            startupCtx.AddProperty("Version", "1.0.0");
            LogCtx.Logger.Information("Application starting", startupCtx);
            
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                using var errorCtx = LogCtx.Set();
                errorCtx.AddProperty("ApplicationName", "YourApp");
                errorCtx.AddProperty("ErrorType", ex.GetType().Name);
                LogCtx.Logger.Fatal("Application failed to start", ex, errorCtx);
                throw;
            }
            finally
            {
                using var shutdownCtx = LogCtx.Set();
                shutdownCtx.AddProperty("ApplicationName", "YourApp");
                LogCtx.Logger.Information("Application shutting down", shutdownCtx);
            }
        }
    }
}
```

---

## 🧪 **Test Integration**

### **Unit Test Setup**

```csharp
using NUnit.Framework;
using Shouldly;
using NLogShared;   // Required for FailsafeLogger
using LogCtxShared; // Required for LogCtx classes

[TestFixture]
public class YourServiceTests
{
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        // ⚠️ MANDATORY INITIALIZATION - Initialize once per test fixture
        FailsafeLogger.Initialize("NLog.config");
    }

    [Test]
    public void ProcessFile_ValidInput_ShouldSucceed()
    {
        // Arrange
        using var testCtx = LogCtx.Set();
        testCtx.AddProperty("TestMethod", nameof(ProcessFile_ValidInput_ShouldSucceed));
        testCtx.AddProperty("TestCategory", "FileProcessing");
        LogCtx.Logger.Information("Test execution started", testCtx);

        // Act
        var processor = new FileProcessor();
        var result = processor.ProcessFile("test.txt");

        // Assert
        result.ShouldBeTrue();
        
        LogCtx.Logger.Information("Test execution completed", testCtx);
    }
}
```

---

## ⚙️ **Configuration**

### **NLog.config Example**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  
  <targets>
    <!-- Console output -->
    <target xsi:type="Console" name="console"
            layout="${longdate} ${level:uppercase=true} ${logger} ${message} ${exception:format=tostring}" />
    
    <!-- SEQ structured logging -->
    <target xsi:type="Seq" name="seq" serverUrl="http://localhost:5341" apiKey="">
      <property name="Application" value="YourApp" />
      <property name="Environment" value="Development" />
    </target>
  </targets>

  <rules>
    <logger name="*" minlevel="Debug" writeTo="console" />
    <logger name="*" minlevel="Information" writeTo="seq" />
  </rules>
</nlog>
```

---

## 🚨 **Critical Usage Notes**

### **MUST DO:**
1. ✅ **Always call** `FailsafeLogger.Initialize("NLog.config")` first
2. ✅ **Include both** `using NLogShared;` and `using LogCtxShared;`
3. ✅ **Use `using` statements** with LogCtx.Set() for proper disposal
4. ✅ **Initialize once** per application/test fixture

### **NEVER DO:**
1. ❌ **Never call** `LogCtx.InitLogCtx()` (this method doesn't exist!)
2. ❌ **Never omit** the required using statements
3. ❌ **Never forget** to dispose LogCtx contexts
4. ❌ **Never initialize** multiple times in the same application

---

## 🔗 **Additional Resources**

- **[Step-0-Integration-Guide.md]** - Detailed integration walkthrough
- **[AI-Code-Generation-Guide.md]** - AI-assisted development patterns
- **[Usage-Patterns-Examples.md]** - Advanced usage examples
- **[Performance-Guide.md]** - Optimization techniques

---

## 📊 **Real-World Usage**

LogCtx is battle-tested in production applications:

- **7-project modular architecture** with centralized logging
- **Git workflow automation** with structured audit trails
- **Unit test execution** with detailed context capture
- **Recent files management** with performance tracking
- **SEQ dashboard** for operational monitoring

### **Key Benefits Demonstrated:**
- ✅ **Zero crashes** from logging operations
- ✅ **Rich structured data** for operational insights
- ✅ **Seamless SEQ integration** for query optimization
- ✅ **Test-friendly patterns** with NUnit/Shouldly
- ✅ **High performance** with minimal overhead

---

**Version:** 0.3.1  
**Last Updated:** October 2025  
**Compatibility:** .NET 8.0+, NLog 6.0.4+