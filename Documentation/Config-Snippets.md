# LogCtx Configuration Snippets

**Reusable configuration files and code snippets for LogCtx - version 0.3.1**

This directory contains standardized configuration snippets that can be referenced from multiple documentation files to eliminate duplication and ensure consistency.

---

## 📁 **Configuration Files**

### **1. NLog.config - Complete Development Configuration**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  
  <!-- Configuration variables -->
  <variable name="logDirectory" value="logs"/>
  <variable name="archiveDirectory" value="logs/archive"/>
  
  <targets>
    <!-- Console output for development -->
    <target xsi:type="Console" name="console"
            layout="${longdate} ${level:uppercase=true:padding=-5} ${logger:shortName=true} ${message} ${exception:format=tostring}" />
    
    <!-- File output with rolling -->
    <target xsi:type="File" name="file"
            fileName="${logDirectory}/${shortdate}.log"
            layout="${longdate} ${level:uppercase=true:padding=-5} ${logger} ${message} ${exception:format=tostring}"
            archiveFileName="${archiveDirectory}/{#}.log"
            archiveEvery="Day"
            archiveNumbering="Rolling"
            maxArchiveFiles="30"
            concurrentWrites="true"
            keepFileOpen="false" />
    
    <!-- SEQ structured logging -->
    <target xsi:type="Seq" name="seq" 
            serverUrl="http://localhost:5341" 
            apiKey="">
      <property name="Application" value="YourAppName" />
      <property name="Environment" value="Development" />
      <property name="MachineName" value="${machinename}" />
      <property name="ProcessId" value="${processid}" />
      <property name="ThreadId" value="${threadid}" />
    </target>

    <!-- Error file for critical issues -->
    <target xsi:type="File" name="errorFile"
            fileName="${logDirectory}/errors-${shortdate}.log"
            layout="${longdate} ${level:uppercase=true} ${logger} ${message} ${exception:format=toString}"
            archiveFileName="${archiveDirectory}/errors-{#}.log"
            archiveEvery="Day"
            archiveNumbering="Rolling"
            maxArchiveFiles="90" />
  </targets>

  <rules>
    <!-- Development: All levels to console -->
    <logger name="*" minlevel="Debug" writeTo="console" />
    
    <!-- All Information+ to main log file -->
    <logger name="*" minlevel="Information" writeTo="file" />
    
    <!-- Structured logging to SEQ -->
    <logger name="*" minlevel="Information" writeTo="seq" />
    
    <!-- Errors to separate file -->
    <logger name="*" minlevel="Error" writeTo="errorFile" />
  </rules>
  
</nlog>
```

### **2. NLog.config - Production Configuration**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  
  <variable name="logDirectory" value="C:/Logs/YourApp"/>
  <variable name="archiveDirectory" value="C:/Logs/YourApp/Archive"/>
  
  <targets>
    <!-- Production file logging -->
    <target xsi:type="File" name="file"
            fileName="${logDirectory}/${shortdate}.log"
            layout="${longdate} ${level:uppercase=true:padding=-5} [${threadid}] ${logger} ${message} ${exception:format=tostring}"
            archiveFileName="${archiveDirectory}/{#}.log"
            archiveEvery="Day"
            archiveNumbering="Rolling"
            maxArchiveFiles="90"
            concurrentWrites="true"
            keepFileOpen="true" />
    
    <!-- SEQ for production monitoring -->
    <target xsi:type="Seq" name="seq" 
            serverUrl="https://seq.yourcompany.com" 
            apiKey="${environment:SEQ_API_KEY}">
      <property name="Application" value="YourAppName" />
      <property name="Environment" value="Production" />
      <property name="Server" value="${machinename}" />
      <property name="Version" value="${assembly-version}" />
    </target>

    <!-- Critical errors via email -->
    <target xsi:type="Mail" name="email"
            smtpServer="smtp.yourcompany.com"
            smtpPort="587"
            smtpAuthentication="Basic"
            smtpUserName="${environment:SMTP_USERNAME}"
            smtpPassword="${environment:SMTP_PASSWORD}"
            enableSsl="true"
            from="noreply@yourcompany.com"
            to="alerts@yourcompany.com"
            subject="[CRITICAL] ${machinename} - ${logger}"
            body="${longdate} ${level:uppercase=true} ${logger} ${message} ${exception:format=toString}"
            layout="${longdate} ${level:uppercase=true} ${logger} ${message} ${exception:format=toString}" />

    <!-- Windows Event Log -->
    <target xsi:type="EventLog" name="eventLog"
            source="YourAppName"
            layout="${message} ${exception:format=toString}" />
  </targets>

  <rules>
    <!-- Production: Information+ to file -->
    <logger name="*" minlevel="Information" writeTo="file" />
    
    <!-- Structured logging to SEQ -->
    <logger name="*" minlevel="Information" writeTo="seq" />
    
    <!-- Critical errors via email -->
    <logger name="*" minlevel="Error" writeTo="email" />
    
    <!-- Fatal errors to Event Log -->
    <logger name="*" minlevel="Fatal" writeTo="eventLog" />
  </rules>
  
</nlog>
```

### **3. NLog.config - Testing Configuration**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  
  <targets>
    <!-- Console output for test debugging -->
    <target xsi:type="Console" name="console"
            layout="${time} ${level:uppercase=true:padding=-5} ${logger:shortName=true} ${message}" />
    
    <!-- Memory target for test verification -->
    <target xsi:type="Memory" name="memory" />
    
    <!-- Test file output -->
    <target xsi:type="File" name="testFile"
            fileName="test-logs/test-${shortdate}.log"
            layout="${time} ${level:uppercase=true} ${logger} ${message} ${exception:format=tostring}"
            deleteOldFileOnStartup="true" />
  </targets>

  <rules>
    <!-- Test: Debug+ to console for visibility -->
    <logger name="*" minlevel="Debug" writeTo="console" />
    
    <!-- All to memory for test assertions -->
    <logger name="*" minlevel="Trace" writeTo="memory" />
    
    <!-- Information+ to test file -->
    <logger name="*" minlevel="Information" writeTo="testFile" />
  </rules>
  
</nlog>
```

---

## 🏗️ **Standard Code Headers**

### **Application File Header**

```csharp
// ✅ STANDARD APPLICATION HEADER - Copy this exactly
using NLogShared;   // Required for FailsafeLogger
using LogCtxShared; // Required for LogCtx classes
using NLog;         // For direct NLog access if needed
using System;       // Standard system imports
using System.Threading.Tasks; // For async operations
```

### **Service File Header**

```csharp
// ✅ STANDARD SERVICE HEADER - Copy this exactly
using NLogShared;   // Required for FailsafeLogger
using LogCtxShared; // Required for LogCtx classes
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
```

### **Test File Header**

```csharp
// ✅ STANDARD TEST HEADER - Copy this exactly
using NUnit.Framework;  // Primary test framework
using Shouldly;         // Assertion library
using NLogShared;       // Required for FailsafeLogger
using LogCtxShared;     // Required for LogCtx classes
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Moq;             // For mocking (if needed)
```

### **Integration Test Header**

```csharp
// ✅ STANDARD INTEGRATION TEST HEADER - Copy this exactly
using NUnit.Framework;
using Shouldly;
using NLogShared;       // Required for FailsafeLogger
using LogCtxShared;     // Required for LogCtx classes
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection; // For DI setup
using Microsoft.Extensions.Configuration;       // For config
```

---

## 📋 **SEQ Configuration Snippets**

### **Basic SEQ Target**

```xml
<target xsi:type="Seq" name="seq" 
        serverUrl="http://localhost:5341" 
        apiKey="">
  <property name="Application" value="YourAppName" />
  <property name="Environment" value="Development" />
</target>
```

### **Advanced SEQ Target with Properties**

```xml
<target xsi:type="Seq" name="seq" 
        serverUrl="http://localhost:5341" 
        apiKey="${environment:SEQ_API_KEY}"
        batchPostingLimit="50"
        queueSizeLimit="100000"
        serverTimeout="00:00:30">
  
  <!-- Static properties -->
  <property name="Application" value="YourAppName" />
  <property name="Environment" value="${environment:ENVIRONMENT:whenEmpty=Development}" />
  <property name="MachineName" value="${machinename}" />
  <property name="ProcessId" value="${processid}" />
  
  <!-- Dynamic properties from context -->
  <property name="Version" value="${assembly-version}" />
  <property name="BuildNumber" value="${environment:BUILD_NUMBER}" />
  <property name="DeploymentSlot" value="${environment:DEPLOYMENT_SLOT}" />
</target>
```

### **SEQ with Conditional Logging**

```xml
<target xsi:type="Seq" name="seq" 
        serverUrl="http://localhost:5341"
        apiKey="">
  
  <!-- Only log structured events with context -->
  <when condition="length('${event-properties:ServiceName}') > 0">
    <property name="Application" value="YourAppName" />
    <property name="HasContext" value="true" />
  </when>
  
  <!-- Default properties for events without context -->
  <property name="Application" value="YourAppName" />
  <property name="HasContext" value="false" />
</target>
```

---

## 🔧 **Project Configuration Snippets**

### **.csproj File Template**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <WarningsAsErrors />
    <WarningsNotAsErrors>CS1591</WarningsNotAsErrors>
  </PropertyGroup>

  <!-- LogCtx Integration -->
  <ItemGroup>
    <Import Project="LogCtx\NLogShared\NLogShared.projitems" Label="Shared" />
    <Import Project="LogCtx\LogCtxShared\LogCtxShared.projitems" Label="Shared" />
  </ItemGroup>

  <!-- NLog Dependencies - Current Versions -->
  <ItemGroup>
    <PackageReference Include="NLog" Version="6.0.4" />
    <PackageReference Include="NLog.Extensions.Logging" Version="6.0.4" />
    <PackageReference Include="NLog.Targets.Seq" Version="6.0.0" />
    <PackageReference Include="NLog.Schema" Version="6.0.4" />
  </ItemGroup>

  <!-- Testing Dependencies -->
  <ItemGroup Condition="'$(Configuration)' == 'Debug'">
    <PackageReference Include="NUnit" Version="4.2.2" />
    <PackageReference Include="NUnit3TestAdapter" Version="4.6.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="Shouldly" Version="4.2.1" />
    <PackageReference Include="Moq" Version="4.20.72" />
  </ItemGroup>

  <!-- Copy NLog.config to output -->
  <ItemGroup>
    <None Update="NLog.config">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </None>
  </ItemGroup>

</Project>
```

### **Directory.Build.props Template**

```xml
<Project>
  
  <PropertyGroup>
    <!-- Global properties -->
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    
    <!-- Version management -->
    <MajorVersion>1</MajorVersion>
    <MinorVersion>0</MinorVersion>
    <PatchVersion>0</PatchVersion>
    <Version>$(MajorVersion).$(MinorVersion).$(PatchVersion)</Version>
    
    <!-- Build configuration -->
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <WarningsAsErrors />
    <WarningsNotAsErrors>CS1591</WarningsNotAsErrors>
  </PropertyGroup>

  <!-- Global NLog package versions -->
  <ItemGroup>
    <GlobalPackageReference Include="NLog" Version="6.0.4" />
    <GlobalPackageReference Include="NLog.Extensions.Logging" Version="6.0.4" />
  </ItemGroup>

</Project>
```

---

## 🧪 **Test Configuration Templates**

### **Test Project .csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <!-- Reference main project -->
  <ItemGroup>
    <ProjectReference Include="..\YourProject\YourProject.csproj" />
  </ItemGroup>

  <!-- LogCtx Integration for Tests -->
  <ItemGroup>
    <Import Project="..\LogCtx\NLogShared\NLogShared.projitems" Label="Shared" />
    <Import Project="..\LogCtx\LogCtxShared\LogCtxShared.projitems" Label="Shared" />
  </ItemGroup>

  <!-- Test Dependencies -->
  <ItemGroup>
    <PackageReference Include="NUnit" Version="4.2.2" />
    <PackageReference Include="NUnit3TestAdapter" Version="4.6.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="Shouldly" Version="4.2.1" />
    <PackageReference Include="Moq" Version="4.20.72" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.1" />
  </ItemGroup>

  <!-- Copy test configuration -->
  <ItemGroup>
    <None Update="NLog.config">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </None>
    <None Update="test-data\**\*">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>

</Project>
```

### **TestSettings.json Template**

```json
{
  "LogCtx": {
    "ConfigFile": "NLog.config",
    "LogLevel": "Debug",
    "EnableConsoleOutput": true,
    "EnableFileOutput": true,
    "LogDirectory": "test-logs"
  },
  "SEQ": {
    "ServerUrl": "http://localhost:5341",
    "ApiKey": "",
    "ApplicationName": "YourApp.Tests",
    "Environment": "Testing"
  },
  "TestData": {
    "Directory": "test-data",
    "CleanupAfterTests": true,
    "CreateTempDirectory": true
  }
}
```

---

## 📊 **Environment-Specific Configurations**

### **Development appsettings.json**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "System": "Information",
      "Microsoft": "Information"
    }
  },
  "LogCtx": {
    "NLogConfigFile": "NLog.config",
    "EnableConsoleLogging": true,
    "EnableSeqLogging": true,
    "SeqServerUrl": "http://localhost:5341"
  }
}
```

### **Production appsettings.json**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "System": "Warning",
      "Microsoft": "Warning"
    }
  },
  "LogCtx": {
    "NLogConfigFile": "NLog.Production.config",
    "EnableConsoleLogging": false,
    "EnableSeqLogging": true,
    "SeqServerUrl": "https://seq.yourcompany.com",
    "EnableEmailAlerts": true
  }
}
```

---

## 🔍 **Configuration Validation Scripts**

### **PowerShell Validation Script**

```powershell
# Validate-LogCtxConfig.ps1
param(
    [string]$ConfigFile = "NLog.config",
    [string]$ProjectFile = "*.csproj"
)

Write-Host "🔍 Validating LogCtx Configuration..." -ForegroundColor Cyan

# Check if NLog.config exists
if (-not (Test-Path $ConfigFile)) {
    Write-Host "❌ $ConfigFile not found!" -ForegroundColor Red
    exit 1
}

# Validate XML structure
try {
    [xml]$config = Get-Content $ConfigFile
    Write-Host "✅ NLog.config is valid XML" -ForegroundColor Green
} catch {
    Write-Host "❌ NLog.config has invalid XML: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Check required targets
$requiredTargets = @("console", "file")
$targets = $config.nlog.targets.target.name

foreach ($required in $requiredTargets) {
    if ($targets -contains $required) {
        Write-Host "✅ Required target '$required' found" -ForegroundColor Green
    } else {
        Write-Host "⚠️ Required target '$required' missing" -ForegroundColor Yellow
    }
}

Write-Host "🎉 Configuration validation completed!" -ForegroundColor Cyan
```

---

## 📋 **Quick Reference**

### **Files to Copy for New Project**

1. ✅ **NLog.config** (choose appropriate version)
2. ✅ **Standard code headers** for your file types
3. ✅ **.csproj modifications** for LogCtx integration
4. ✅ **Directory.Build.props** for consistent versions

### **Version Compatibility Matrix**

| **LogCtx** | **NLog** | **.NET** | **Status** |
|------------|----------|----------|------------|
| 0.3.1 | 6.0.4 | 8.0+ | ✅ Current |
| 0.3.1 | 6.0.x | 8.0+ | ✅ Supported |
| 0.3.x | 5.x.x | 6.0+ | ⚠️ Legacy |

---

**Version:** 0.3.1  
**Last Updated:** October 2025  
**Usage:** Reference these snippets from documentation files to eliminate duplication