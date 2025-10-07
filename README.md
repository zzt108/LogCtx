# LogCtx - Structured Logging Library
*Contextual logging with automatic source location capture for .NET applications*

[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![NLog](https://img.shields.io/badge/NLog-5.3.4-green.svg)](https://nlog-project.org/)
[![SEQ](https://img.shields.io/badge/SEQ-Ready-orange.svg)](https://datalust.co/seq)

## 🎯 **Quick Start**

LogCtx provides **structured logging with contextual properties** that automatically capture source location (file, method, line) and allow you to enrich logs with custom data. Perfect for **SEQ integration** and AI-assisted development.

### **Initialize Once**
```csharp
// Program.cs or App.xaml.cs - Initialize once per application
using NLogShared;
FailsafeLogger.Initialize("NLog.config");
```

### **Use Everywhere**
```csharp
using LogCtxShared;
using NLogShared;

public class FileProcessor
{
    public async Task ProcessFileAsync(string filePath)
    {
        // ✅ Each operation gets its own context
        using var ctx = LogCtx.Set(new Props()
            .Add("FilePath", filePath)
            .Add("Operation", "ProcessFile"));
            
        LogCtx.Logger.Info("File processing started");
        
        try
        {
            var content = await File.ReadAllTextAsync(filePath);
            ctx.Add("FileSize", content.Length);
            
            // Your logic here...
            
            LogCtx.Logger.Info("File processing completed");
        }
        catch (Exception ex)
        {
            ctx.Add("ErrorType", ex.GetType().Name);
            LogCtx.Logger.Error(ex, "File processing failed");
            throw;
        }
    }
}
```

## 🏗️ **Architecture**

LogCtx consists of **3 shared projects** that integrate via Git submodule:

```
LogCtx/
├── LogCtxShared/           # ✅ Core interfaces (ALWAYS include)
│   ├── LogCtx.cs          # Main context manager
│   ├── Props.cs           # Fluent property builder
│   ├── ILogCtxLogger.cs   # Logger abstraction
│   └── JsonExtensions.cs  # JSON serialization helpers
├── NLogShared/            # ✅ NLog adapter (PRIMARY)
│   └── CtxLogger.cs       # NLog implementation
├── SeriLogShared/         # ✅ Serilog adapter (SECONDARY)
│   └── CtxLogger.cs       # Serilog implementation
└── Documentation/         # ✅ AI-optimized guides
    ├── AI-Code-Generation-Guide.md
    ├── SEQ-Configuration-Guide.md
    └── Integration-Guide.md
```

## 🚀 **Integration Methods**

### **Method 1: Git Submodule (Recommended)**

```bash
# Add LogCtx as submodule
git submodule add https://github.com/zzt108/LogCtx.git LogCtx
git submodule update --init --recursive

# Reference in your .csproj
# <Import Project="LogCtx\LogCtxShared\LogCtxShared.projitems" Label="Shared" />
# <Import Project="LogCtx\NLogShared\NLogShared.projitems" Label="Shared" />
```

### **Method 2: Direct Integration**
See [Documentation/Step-0-Integration-Guide.md](Documentation/Step-0-Integration-Guide.md) for complete setup instructions.

## 🎪 **SEQ Integration (Primary Target)**

LogCtx is **optimized for SEQ** structured logging with rich contextual data:

### **NLog.config for SEQ**
```xml
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd">
  <targets>
    <target xsi:type="Seq" 
            name="seq" 
            serverUrl="http://localhost:5341"
            compactMode="true">
      <property name="Application" value="YourApp" />
      <property name="Environment" value="Development" />
      <property name="MachineName" value="${machinename}" />
    </target>
    
    <target xsi:type="Console" 
            name="console"
            layout="${time} [${level}] ${logger}: ${message}" />
  </targets>
  
  <rules>
    <logger name="*" minlevel="Debug" writeTo="seq" />
    <logger name="*" minlevel="Info" writeTo="console" />
  </rules>
</nlog>
```

### **SEQ Query Examples**
```sql
-- Find all file processing operations
Operation = 'ProcessFile'

-- Performance analysis  
Duration > 1000

-- Error tracking by type
ErrorType is not null | group by ErrorType | sort by count desc
```

## 🤖 **AI Assistant Ready**

LogCtx is designed for **AI-assisted development** with comprehensive patterns and examples:

- **GitHub Copilot**: Auto-completes LogCtx patterns
- **ChatGPT/Claude**: Follows structured logging conventions  
- **Perplexity**: Deep research with copy-paste ready code
- **Gemini**: Contextual code generation

See [Documentation/AI-Code-Generation-Guide.md](Documentation/AI-Code-Generation-Guide.md) for detailed AI integration patterns.

## 📚 **Documentation**

### **🚀 Getting Started**
- [Step 0: Integration Guide](Documentation/Step-0-Integration-Guide.md) - Git submodule setup
- [AI Code Generation Guide](Documentation/AI-Code-Generation-Guide.md) - Copy-paste patterns for AI assistants
- [SEQ Configuration Guide](Documentation/SEQ-Configuration-Guide.md) - Complete SEQ setup

### **📖 Reference**
- [API Complete Reference](Documentation/API-Complete-Reference.md) - All methods and classes
- [Usage Patterns Examples](Documentation/Usage-Patterns-Examples.md) - Real-world examples
- [NLog Configuration Examples](Documentation/NLog-Configuration-Examples.md) - Multiple environments

### **🛠️ Advanced**
- [Best Practices](Documentation/Best-Practices.md) - Do's and don'ts
- [Testing Patterns](Documentation/Testing-Patterns.md) - NUnit/Shouldly integration
- [Troubleshooting](Documentation/Troubleshooting.md) - Common issues and solutions
- [Migration Guide](Documentation/Migration-From-Direct-NLog.md) - Upgrade existing code

## 🏆 **Real-World Usage**

LogCtx is battle-tested in production applications:

### **VecTool Project**
- **7-project modular architecture** with centralized logging
- **Git workflow automation** with structured audit trails  
- **Unit test execution** with detailed context capture
- **Recent files management** with performance tracking
- **SEQ dashboard** for operational monitoring

### **Key Benefits Demonstrated**
- ✅ **Zero-config initialization** that never throws exceptions
- ✅ **Automatic source location** capture (file:line) 
- ✅ **Fluent property building** with method chaining
- ✅ **Exception context enrichment** preserving stack traces
- ✅ **SEQ query optimization** with structured properties
- ✅ **Test-friendly patterns** with NUnit/Shouldly

## 🧪 **Testing Integration**

```csharp
[TestFixture]
public class FileProcessorTests
{
    [OneTimeSetUp]
    public void Setup()
    {
        // ✅ Initialize LogCtx once per test fixture
        FailsafeLogger.Initialize();
    }
    
    [Test]
    public void ProcessFile_ValidInput_ShouldSucceed()
    {
        // Arrange
        using var testCtx = LogCtx.Set(new Props()
            .Add("TestMethod", nameof(ProcessFile_ValidInput_ShouldSucceed))
            .Add("Category", "FileProcessing"));
            
        LogCtx.Logger.Info("Test execution started");
        
        // Act & Assert
        var processor = new FileProcessor();
        var result = processor.ProcessFile("test.txt");
        
        result.Should().BeTrue();
        LogCtx.Logger.Info("Test execution completed");
    }
}
```

## 🔮 **Roadmap**

### **Phase 1: Documentation Complete** *(Current)*
- ✅ AI-optimized documentation  
- ✅ SEQ integration guides
- ✅ Real-world usage patterns
- 🔄 API reference completion

### **Phase 2: WinUI 3 Support** *(Planned)*
- 📋 Dependency injection integration
- 📋 WinUI 3-specific logging targets  
- 📋 Application lifecycle logging
- 📋 Enhanced SEQ configuration helpers

*See [WinUI3-Upgrade-Plan.md](Documentation/WinUI3-Upgrade-Plan.md) for detailed Phase 2 planning.*

## ⚡ **Performance**

LogCtx is designed for **high-performance applications**:

- **Failsafe initialization** - Never throws, always works
- **Minimal memory allocation** - Reuses context objects
- **Async-friendly** - No blocking operations
- **SEQ-optimized** - Structured properties for fast queries
- **Source location caching** - Compile-time optimizations

## 🤝 **Contributing**

This is an **internal library** for personal projects. The codebase follows strict conventions:

- **English-only** code, variables, and comments
- **SOLID principles** throughout the architecture  
- **NUnit + Shouldly** for all testing
- **Structured logging** as a first-class citizen
- **AI assistant compatibility** in all documentation

## 📜 **License**

Internal use only. Not for external distribution.

---

## 🎯 **Key Takeaways**

1. **Initialize once**: `FailsafeLogger.Initialize("NLog.config")` in Program.cs
2. **Context per operation**: `using var ctx = LogCtx.Set(...)` for each significant action
3. **Enrich before logging**: Add properties to context, then log
4. **SEQ integration**: Optimized for structured queries and dashboards
5. **AI-ready**: Comprehensive patterns for coding assistants

**Start here**: [Documentation/AI-Code-Generation-Guide.md](Documentation/AI-Code-Generation-Guide.md) 🚀