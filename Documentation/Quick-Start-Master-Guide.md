# LogCtx Quick Start Master Guide

**Centralized quick start examples and patterns - version 0.3.1**

This guide contains the **MASTER COPIES** of all quick start examples. Other documentation files should reference this guide instead of duplicating examples.

---

## 🚨 **CRITICAL: Master Initialization Pattern**

### **❌ NEVER USE - These Methods Don't Exist:**
```csharp
LogCtx.InitLogCtx();        // ❌ COMPILATION ERROR - Method doesn't exist!
LogCtx.Initialize();        // ❌ COMPILATION ERROR - Method doesn't exist!
LogContext.Setup();         // ❌ COMPILATION ERROR - Wrong library!
```

### **✅ ONLY CORRECT PATTERN - Copy This Exactly:**

```csharp
// ✅ REQUIRED USING STATEMENTS - BOTH NEEDED!
using NLogShared;   // Required for FailsafeLogger
using LogCtxShared; // Required for LogCtx classes

// ✅ MANDATORY INITIALIZATION - ONLY VALID METHOD!
FailsafeLogger.Initialize("NLog.config");
```

⚠️ **CRITICAL WARNING:** Without proper initialization, `LogCtx.Logger` will be **null** and crash your application on the first logging call!

---

## 🚀 **Master Quick Start Examples**

### **Example 1: Complete Application Setup**

```csharp
// ✅ MASTER APPLICATION TEMPLATE - Copy this exactly
using NLogShared;   // Required for FailsafeLogger
using LogCtxShared; // Required for LogCtx classes
using NLog;
using System;

namespace YourApplication
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            try
            {
                // ⚠️ STEP 1: Initialize LogCtx FIRST - Before any other operations!
                FailsafeLogger.Initialize("NLog.config");
                
                // ✅ STEP 2: Create application startup context
                using var startupCtx = LogCtx.Set(); // Automatic source location capture
                startupCtx.AddProperty("ApplicationName", "YourApplication");
                startupCtx.AddProperty("Version", "1.0.0");
                startupCtx.AddProperty("Environment", GetEnvironment());
                startupCtx.AddProperty("StartupTime", DateTime.UtcNow);
                LogCtx.Logger.Information("Application startup initiated", startupCtx);

                // ✅ STEP 3: Initialize your services
                InitializeServices(startupCtx);
                
                // ✅ STEP 4: Run your application
                RunApplication();
                
                LogCtx.Logger.Information("Application startup completed", startupCtx);
            }
            catch (Exception ex)
            {
                HandleCriticalError(ex);
                throw;
            }
            finally
            {
                // Graceful shutdown logging
                using var shutdownCtx = LogCtx.Set();
                shutdownCtx.AddProperty("ApplicationName", "YourApplication");
                shutdownCtx.AddProperty("ShutdownTime", DateTime.UtcNow);
                LogCtx.Logger.Information("Application shutdown completed", shutdownCtx);
            }
        }

        private static string GetEnvironment()
        {
            #if DEBUG
                return "Development";
            #else
                return "Production";
            #endif
        }

        private static void InitializeServices(LogCtx parentCtx)
        {
            // Inherit context from parent for related operations
            parentCtx.AddProperty("InitializationStep", "Services");
            LogCtx.Logger.Information("Initializing application services", parentCtx);
            
            // Your service initialization here...
            
            LogCtx.Logger.Information("Service initialization completed", parentCtx);
        }

        private static void RunApplication()
        {
            // Your main application code here
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        private static void HandleCriticalError(Exception ex)
        {
            try
            {
                using var errorCtx = LogCtx.Set();
                errorCtx.AddProperty("ApplicationName", "YourApplication");
                errorCtx.AddProperty("ErrorStage", "Startup");
                errorCtx.AddProperty("ErrorType", ex.GetType().Name);
                errorCtx.AddProperty("ErrorMessage", ex.Message);
                LogCtx.Logger.Fatal("Critical application startup failure", ex, errorCtx);
            }
            catch
            {
                // Last resort - console fallback if LogCtx fails
                Console.WriteLine($"FATAL: Application startup failed - {ex}");
            }
        }
    }
}
```

### **Example 2: Service Method with Full Error Handling**

```csharp
// ✅ MASTER SERVICE METHOD TEMPLATE - Copy this exactly
using NLogShared;   // Required for FailsafeLogger
using LogCtxShared; // Required for LogCtx classes
using System;
using System.Threading.Tasks;

public class UserService
{
    public async Task<User> GetUserByIdAsync(int userId)
    {
        // ✅ STEP 1: Create operation context with automatic source capture
        using var ctx = LogCtx.Set(); // Captures file/line/method automatically
        ctx.AddProperty("ServiceName", nameof(UserService));
        ctx.AddProperty("Operation", nameof(GetUserByIdAsync));
        ctx.AddProperty("UserId", userId);
        ctx.AddProperty("RequestId", Guid.NewGuid().ToString());
        
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        LogCtx.Logger.Information("User retrieval operation started", ctx);

        try
        {
            // ✅ STEP 2: Input validation with logging
            if (userId <= 0)
            {
                ctx.AddProperty("ValidationResult", "InvalidUserId");
                ctx.AddProperty("ValidationError", "UserId must be positive");
                LogCtx.Logger.Warning("Invalid user ID provided", ctx);
                throw new ArgumentException("User ID must be positive", nameof(userId));
            }

            ctx.AddProperty("ValidationResult", "Success");
            LogCtx.Logger.Information("Input validation passed", ctx);

            // ✅ STEP 3: Business logic with performance tracking
            var user = await RetrieveUserFromDatabase(userId);
            stopwatch.Stop();

            if (user == null)
            {
                ctx.AddProperty("OperationResult", "UserNotFound");
                ctx.AddProperty("LookupDurationMs", stopwatch.ElapsedMilliseconds);
                LogCtx.Logger.Information("User not found in database", ctx);
                throw new UserNotFoundException($"User with ID {userId} not found");
            }

            // ✅ STEP 4: Success logging with metrics
            ctx.AddProperty("OperationResult", "Success");
            ctx.AddProperty("UserName", user.Name);
            ctx.AddProperty("UserEmail", user.Email);
            ctx.AddProperty("LookupDurationMs", stopwatch.ElapsedMilliseconds);
            LogCtx.Logger.Information("User retrieval completed successfully", ctx);

            return user;
        }
        catch (Exception ex)
        {
            // ✅ STEP 5: Error context with comprehensive information
            stopwatch.Stop();
            ctx.AddProperty("OperationResult", "Error");
            ctx.AddProperty("ErrorType", ex.GetType().Name);
            ctx.AddProperty("ErrorMessage", ex.Message);
            ctx.AddProperty("DurationBeforeFailureMs", stopwatch.ElapsedMilliseconds);
            LogCtx.Logger.Error("User retrieval operation failed", ex, ctx);
            throw; // Re-throw to preserve stack trace
        }
    }

    private async Task<User?> RetrieveUserFromDatabase(int userId)
    {
        // Your database access code here
        await Task.Delay(100); // Simulate database call
        return new User { Id = userId, Name = "Test User", Email = "test@example.com" };
    }
}
```

### **Example 3: Unit Test with Complete Setup**

```csharp
// ✅ MASTER UNIT TEST TEMPLATE - Copy this exactly
using NUnit.Framework;
using Shouldly;
using NLogShared;   // Required for FailsafeLogger
using LogCtxShared; // Required for LogCtx classes
using System;
using System.Threading.Tasks;

[TestFixture]
public class UserServiceTests
{
    private UserService userService = null!;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        // ⚠️ CRITICAL: Initialize LogCtx once per test fixture
        FailsafeLogger.Initialize("NLog.config");
        
        using var setupCtx = LogCtx.Set();
        setupCtx.AddProperty("TestFixture", nameof(UserServiceTests));
        setupCtx.AddProperty("TestFramework", "NUnit");
        setupCtx.AddProperty("SetupTime", DateTime.UtcNow);
        LogCtx.Logger.Information("Test fixture initialization started", setupCtx);

        // Initialize test dependencies
        userService = new UserService();
        
        LogCtx.Logger.Information("Test fixture initialization completed", setupCtx);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        using var teardownCtx = LogCtx.Set();
        teardownCtx.AddProperty("TestFixture", nameof(UserServiceTests));
        teardownCtx.AddProperty("TeardownTime", DateTime.UtcNow);
        LogCtx.Logger.Information("Test fixture teardown completed", teardownCtx);
    }

    [Test]
    [Category("UserService")]
    public async Task GetUserByIdAsync_ValidUserId_ShouldReturnUser()
    {
        // ✅ Test execution context
        using var testCtx = LogCtx.Set();
        testCtx.AddProperty("TestClass", nameof(UserServiceTests));
        testCtx.AddProperty("TestMethod", nameof(GetUserByIdAsync_ValidUserId_ShouldReturnUser));
        testCtx.AddProperty("TestCategory", "UserService");
        testCtx.AddProperty("TestScenario", "ValidUserId");
        LogCtx.Logger.Information("Test execution started", testCtx);

        try
        {
            // Arrange
            const int validUserId = 123;
            testCtx.AddProperty("TestUserId", validUserId);
            LogCtx.Logger.Information("Test data arranged", testCtx);

            // Act
            var result = await userService.GetUserByIdAsync(validUserId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(validUserId);
            result.Name.Should().NotBeNullOrEmpty();
            result.Email.Should().NotBeNullOrEmpty();

            testCtx.AddProperty("ActualUserId", result.Id);
            testCtx.AddProperty("ActualUserName", result.Name);
            testCtx.AddProperty("AssertionResult", "Success");
            LogCtx.Logger.Information("Test assertions passed", testCtx);
            
            LogCtx.Logger.Information("Test execution completed successfully", testCtx);
        }
        catch (Exception ex)
        {
            // Test failure context
            using var errorCtx = LogCtx.Set();
            errorCtx.AddProperty("TestClass", nameof(UserServiceTests));
            errorCtx.AddProperty("TestMethod", nameof(GetUserByIdAsync_ValidUserId_ShouldReturnUser));
            errorCtx.AddProperty("ErrorType", ex.GetType().Name);
            errorCtx.AddProperty("TestResult", "Failed");
            LogCtx.Logger.Error("Test execution failed", ex, errorCtx);
            throw; // Re-throw for NUnit
        }
    }

    [Test]
    [Category("UserService")]
    public async Task GetUserByIdAsync_InvalidUserId_ShouldThrowArgumentException()
    {
        using var testCtx = LogCtx.Set();
        testCtx.AddProperty("TestClass", nameof(UserServiceTests));
        testCtx.AddProperty("TestMethod", nameof(GetUserByIdAsync_InvalidUserId_ShouldThrowArgumentException));
        testCtx.AddProperty("TestScenario", "InvalidUserId");
        LogCtx.Logger.Information("Test execution started", testCtx);

        try
        {
            // Arrange
            const int invalidUserId = -1;
            testCtx.AddProperty("InvalidUserId", invalidUserId);

            // Act & Assert - Expecting exception
            var exception = await Should.ThrowAsync<ArgumentException>(
                () => userService.GetUserByIdAsync(invalidUserId));

            // Verify exception details
            exception.ParamName.Should().Be("userId");
            exception.Message.Should().Contain("must be positive");

            testCtx.AddProperty("ExceptionType", exception.GetType().Name);
            testCtx.AddProperty("ExceptionMessage", exception.Message);
            testCtx.AddProperty("AssertionResult", "Success");
            LogCtx.Logger.Information("Exception assertions passed", testCtx);
            
            LogCtx.Logger.Information("Test execution completed successfully", testCtx);
        }
        catch (Exception ex)
        {
            using var errorCtx = LogCtx.Set();
            errorCtx.AddProperty("TestClass", nameof(UserServiceTests));
            errorCtx.AddProperty("TestMethod", nameof(GetUserByIdAsync_InvalidUserId_ShouldThrowArgumentException));
            errorCtx.AddProperty("ErrorType", ex.GetType().Name);
            LogCtx.Logger.Error("Test execution failed", ex, errorCtx);
            throw;
        }
    }
}
```

### **Example 4: Simple Console Application**

```csharp
// ✅ MASTER CONSOLE APP TEMPLATE - Copy this exactly
using NLogShared;   // Required for FailsafeLogger
using LogCtxShared; // Required for LogCtx classes
using System;

namespace SimpleLogCtxExample
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            // ⚠️ STEP 1: Initialize LogCtx - MANDATORY!
            FailsafeLogger.Initialize("NLog.config");

            // ✅ STEP 2: Simple operation with context
            using var ctx = LogCtx.Set();
            ctx.AddProperty("ApplicationName", "SimpleExample");
            ctx.AddProperty("ArgumentCount", args.Length);
            ctx.AddProperty("StartTime", DateTime.UtcNow);
            LogCtx.Logger.Information("Console application started", ctx);

            try
            {
                // ✅ STEP 3: Process command line arguments
                ProcessArguments(args);
                
                // ✅ STEP 4: Success completion
                ctx.AddProperty("Result", "Success");
                LogCtx.Logger.Information("Console application completed successfully", ctx);
            }
            catch (Exception ex)
            {
                // ✅ STEP 5: Error handling
                using var errorCtx = LogCtx.Set();
                errorCtx.AddProperty("ApplicationName", "SimpleExample");
                errorCtx.AddProperty("ErrorType", ex.GetType().Name);
                errorCtx.AddProperty("ErrorMessage", ex.Message);
                LogCtx.Logger.Error("Console application failed", ex, errorCtx);
                
                Environment.Exit(1);
            }
        }

        private static void ProcessArguments(string[] args)
        {
            using var processCtx = LogCtx.Set();
            processCtx.AddProperty("Operation", "ProcessArguments");
            processCtx.AddProperty("ArgumentCount", args.Length);
            
            if (args.Length == 0)
            {
                processCtx.AddProperty("Result", "NoArguments");
                LogCtx.Logger.Information("No arguments provided", processCtx);
                Console.WriteLine("Hello World! (No arguments provided)");
                return;
            }

            // Process each argument
            for (int i = 0; i < args.Length; i++)
            {
                processCtx.AddProperty($"Arg{i}", args[i]);
                LogCtx.Logger.Information($"Processing argument {i}: {args[i]}", processCtx);
                Console.WriteLine($"Argument {i}: {args[i]}");
            }

            processCtx.AddProperty("Result", "AllArgumentsProcessed");
            LogCtx.Logger.Information("All arguments processed successfully", processCtx);
        }
    }
}
```

---

## 📋 **Standard Using Statements Reference**

### **For Application Files:**
```csharp
using NLogShared;   // Required for FailsafeLogger
using LogCtxShared; // Required for LogCtx classes
using NLog;         // For direct NLog access if needed
using System;
using System.Threading.Tasks;
```

### **For Service Files:**
```csharp
using NLogShared;   // Required for FailsafeLogger
using LogCtxShared; // Required for LogCtx classes
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
```

### **For Test Files:**
```csharp
using NUnit.Framework;
using Shouldly;
using NLogShared;   // Required for FailsafeLogger
using LogCtxShared; // Required for LogCtx classes
using System;
using System.Threading.Tasks;
using Moq; // If using mocking
```

---

## 🚨 **Critical Initialization Checklist**

### **✅ MUST DO:**
1. **Always call** `FailsafeLogger.Initialize("NLog.config")` first
2. **Include both** required using statements in every file
3. **Use `using` statements** with `LogCtx.Set()` for proper disposal
4. **Initialize only once** per application or test fixture
5. **Add meaningful properties** to every context

### **❌ NEVER DO:**
1. **Never call** non-existent methods like `LogCtx.InitLogCtx()`
2. **Never omit** required using statements
3. **Never forget** to dispose contexts (always use `using`)
4. **Never initialize** multiple times in the same process
5. **Never ignore** exception context enrichment

---

## 📊 **Property Naming Standards**

### **Required Properties (Use These Names):**
- `ServiceName` - Name of the service/class
- `Operation` - Name of the method/operation
- `RequestId` - Correlation ID for request tracking
- `ErrorType` - Exception type name (`ex.GetType().Name`)
- `UserId` - User identifier for user-specific operations

### **Performance Properties:**
- `DurationMs` - Operation duration in milliseconds
- `StartTime` - Operation start timestamp
- `EndTime` - Operation completion timestamp
- `RecordCount` - Number of records processed
- `FileSize` - File size for file operations

### **Context Properties:**
- `ApplicationName` - Name of the application
- `Version` - Application version
- `Environment` - Environment name (Development/Production)
- `MachineName` - Server/machine identifier

---

## 🔗 **Reference Documentation**

When referencing these examples in other documentation:

```markdown
For complete initialization examples, see [Quick-Start-Master-Guide-CORRECTED.md].

For detailed configuration options, see [Config-Snippets-CORRECTED.md].

For comprehensive API reference, see [API-Complete-Reference-CORRECTED.md].
```

---

**Version:** 0.3.1  
**Last Updated:** October 2025  
**Usage:** This is the MASTER copy - reference from other docs, do not duplicate!