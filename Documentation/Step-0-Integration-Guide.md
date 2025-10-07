# LogCtx Integration Guide - Step 0
*How to integrate LogCtx into your host project properly*

## 🎯 **LogCtx Architecture Overview**

LogCtx consists of **3 shared projects** that work together to provide structured logging:

```
LogCtx/
├── LogCtxShared.projitems      # ✅ Core interfaces and classes (ALWAYS include)
├── NLogShared.projitems        # ✅ NLog adapter (include if using NLog)
└── SeriLogShared.projitems     # ✅ Serilog adapter (include if using Serilog)
```

## 🚀 **Integration Method: Git Submodule**

### **Step 1: Add LogCtx as Git Submodule**

In your host project's root directory:

```bash
# Add LogCtx as a submodule
git submodule add https://github.com/zzt108/LogCtx.git LogCtx

# Initialize and update the submodule
git submodule init
git submodule update

# Alternative: Clone with submodules
git submodule update --init --recursive
```

Your `.gitmodules` file should look like this:

```ini
[submodule "LogCtx"]
	path = LogCtx
	url = https://github.com/zzt108/LogCtx.git
	branch = main
```

### **Step 2: Reference Shared Projects in Your Host Project**

In your host project's `.csproj` file, add the appropriate shared project references:

#### **For NLog Users (Recommended)**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <!-- ✅ Step 2a: Import LogCtx Shared Projects -->
  <Import Project="LogCtx\LogCtxShared\LogCtxShared.projitems" Label="Shared" />
  <Import Project="LogCtx\NLogShared\NLogShared.projitems" Label="Shared" />

  <ItemGroup>
    <!-- ✅ Step 2b: Add NLog NuGet packages -->
    <PackageReference Update="NLog" Version="5.3.4" />
    <PackageReference Update="NLog.Targets.Seq" Version="4.0.2" />
    <PackageReference Update="Newtonsoft.Json" Version="13.0.4" />
  </ItemGroup>

</Project>
```

#### **For Serilog Users**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <!-- ✅ Step 2a: Import LogCtx Shared Projects -->
  <Import Project="LogCtx\LogCtxShared\LogCtxShared.projitems" Label="Shared" />
  <Import Project="LogCtx\SeriLogShared\SeriLogShared.projitems" Label="Shared" />

  <ItemGroup>
    <!-- ✅ Step 2b: Add Serilog NuGet packages -->
    <PackageReference Include="Serilog" Version="4.0.2" />
    <PackageReference Include="Serilog.Extensions.Logging" Version="8.0.0" />
    <PackageReference Include="Serilog.Settings.Configuration" Version="8.0.2" />
    <PackageReference Include="Serilog.Sinks.Console" Version="6.0.0" />
    <PackageReference Include="Serilog.Sinks.Seq" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="8.0.0" />
    <PackageReference Include="Newtonsoft.Json" Version="13.0.4" />
  </ItemGroup>

</Project>
```

## 🔧 **Namespace Usage in Host Project**

### **✅ For NLog Users**
```csharp
using LogCtxShared;    // Core LogCtx functionality
using NLogShared;      // NLog adapter
using NLog;
```

### **✅ For Serilog Users**  
```csharp
using LogCtxShared;    // Core LogCtx functionality
using SeriLogShared;   // Serilog adapter
using Serilog;
```

## 📁 **Project Structure After Integration**

```
YourHostProject/
├── YourHostProject.csproj          # ✅ Contains Import statements
├── LogCtx/                         # ✅ Git submodule directory
│   ├── LogCtxShared/
│   │   ├── LogCtxShared.projitems
│   │   ├── LogCtx.cs               # Core LogCtx class
│   │   ├── ILogCtxLogger.cs        # Logger interface
│   │   ├── Props.cs                # Context properties
│   │   └── JsonExtensions.cs       # JSON helpers
│   ├── NLogShared/
│   │   ├── NLogShared.projitems
│   │   └── CtxLogger.cs            # NLog adapter implementation
│   └── SeriLogShared/
│       ├── SeriLogShared.projitems  
│       └── CtxLogger.cs            # Serilog adapter implementation
├── Program.cs                      # Your app entry point
└── ...
```

## 🎯 **Example: Complete NLog Integration**

### **1. Host Project Setup**

**YourProject.csproj:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <!-- ✅ LogCtx Integration -->
  <Import Project="LogCtx\LogCtxShared\LogCtxShared.projitems" Label="Shared" />
  <Import Project="LogCtx\NLogShared\NLogShared.projitems" Label="Shared" />

  <ItemGroup>
    <PackageReference Update="NLog" Version="5.3.4" />
    <PackageReference Update="NLog.Targets.Seq" Version="4.0.2" />
    <PackageReference Update="Newtonsoft.Json" Version="13.0.4" />
  </ItemGroup>
</Project>
```

### **2. NLog Configuration**

**NLog.config:**
```xml
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  
  <targets>
    <target xsi:type="Seq" 
            name="seq" 
            serverUrl="http://localhost:5341"
            compactMode="true">
      <property name="Application" value="YourAppName" />
      <property name="Environment" value="Development" />
    </target>
    
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

### **3. Program.cs Usage**

```csharp
using LogCtxShared;
using NLogShared;  
using NLog;

class Program
{
    private static readonly CtxLogger log = new();

    static async Task Main(string[] args)
    {
        // ✅ Initialize LogCtx once at application startup
        LogCtx.InitLogCtx();
        
        // ✅ Application startup context
        using var startupCtx = LogCtx.Set();
        startupCtx.AddProperty("ApplicationName", "YourApp");
        startupCtx.AddProperty("Version", "1.0.0");
        LogCtx.LogInformation("Application starting", startupCtx);
        
        await ProcessDataAsync();
        
        LogCtx.LogInformation("Application completed", startupCtx);
    }
    
    static async Task ProcessDataAsync()
    {
        // ✅ Each method gets its own context
        using var ctx = LogCtx.Set();
        ctx.AddProperty("Operation", "ProcessData");
        LogCtx.LogInformation("Processing started", ctx);
        
        try
        {
            // Your business logic here...
            await Task.Delay(100);
            
            LogCtx.LogInformation("Processing completed successfully", ctx);
        }
        catch (Exception ex)
        {
            ctx.AddProperty("ErrorType", ex.GetType().Name);
            LogCtx.LogError("Processing failed", ex, ctx);
            throw;
        }
    }
}
```

## ⚠️ **Critical Integration Notes**

### **✅ DO:**
1. **Always include LogCtxShared.projitems** - This is the core
2. **Include ONE logging adapter** - Either NLogShared OR SeriLogShared (not both)
3. **Use `LogCtx.InitLogCtx()`** once at application startup
4. **Use proper namespaces** - `LogCtxShared` + `NLogShared`/`SeriLogShared`

### **❌ DON'T:**
1. **Don't reference LogCtx as a NuGet package** - Use git submodule only
2. **Don't include both adapters** - Choose NLog OR Serilog, not both
3. **Don't use direct NLog/Serilog** - Always go through LogCtx
4. **Don't forget submodule init** - Your CI/CD needs `git submodule update --init`

## 🧪 **Testing Integration**

### **Unit Test Project Setup**

**TestProject.csproj:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <!-- ✅ Same LogCtx imports as main project -->
  <Import Project="../LogCtx/LogCtxShared/LogCtxShared.projitems" Label="Shared" />
  <Import Project="../LogCtx/NLogShared/NLogShared.projitems" Label="Shared" />

  <ItemGroup>
    <PackageReference Include="NUnit" Version="4.2.2" />
    <PackageReference Include="Shouldly" Version="4.2.0" />
    <PackageReference Include="NLog" Version="5.3.4" />
    <!-- Test packages... -->
  </ItemGroup>
</Project>
```

### **Test Usage Pattern**
```csharp
using NUnit.Framework;
using Shouldly;
using LogCtxShared;
using NLogShared;

[TestFixture]
public class ServiceTests
{
    [OneTimeSetUp]
    public void Setup()
    {
        // ✅ Initialize LogCtx once per test fixture
        LogCtx.InitLogCtx();
    }
    
    [Test]
    public void ProcessData_ValidInput_ShouldSucceed()
    {
        // Arrange
        using var testCtx = LogCtx.Set();
        testCtx.AddProperty("TestMethod", nameof(ProcessData_ValidInput_ShouldSucceed));
        LogCtx.LogInformation("Test starting", testCtx);
        
        // Act & Assert
        var result = ProcessData("test-input");
        result.ShouldNotBeNull();
        
        LogCtx.LogInformation("Test completed successfully", testCtx);
    }
}
```

## 🚀 **CI/CD Considerations**

### **Build Scripts**
```bash
#!/bin/bash
# Ensure submodules are initialized in CI/CD
git submodule update --init --recursive

# Build as normal
dotnet build YourProject.sln
dotnet test YourProject.sln
```

### **Dockerfile**
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# ✅ Copy submodule files
COPY LogCtx/ LogCtx/
COPY YourProject.csproj YourProject/
COPY . YourProject/

RUN dotnet restore YourProject/YourProject.csproj
RUN dotnet build YourProject/YourProject.csproj
```

---

**Confidence Level: 10/10** - This integration guide covers the complete LogCtx setup pattern used in VecTool! 🎯

The key insight is that LogCtx uses **shared projects (.projitems)** rather than traditional project references, which is why the `Import` statements are critical in the host project's .csproj file.