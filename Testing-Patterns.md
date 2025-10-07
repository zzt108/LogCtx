# LogCtx Testing Patterns Guide
*Comprehensive testing strategies with NUnit, Shouldly, and LogCtx integration*

## 🎯 **Testing Philosophy**

LogCtx testing follows **Test-Driven Development (TDD)** principles with **structured logging as a first-class testing concern**. This guide consolidates patterns from VecTool's comprehensive test suite, which includes **300+ tests** across unit, integration, and UI automation scenarios.

### **Core Testing Principles**
- **NUnit + Shouldly**: Primary testing framework with expressive assertions
- **LogCtx Integration**: Every test execution tracked with structured context
- **Defensive Testing**: Tests never crash on logging failures
- **English Throughout**: All test names, variables, and comments in English
- **Naming Convention**: `MethodName_Scenario_ExpectedBehavior`

---

## 🏗️ **Test Project Setup**

### **Test Project Structure (VecTool Pattern)**

```
UnitTests/
├── UnitTests.csproj              # Test project configuration
├── AssemblyAttributes.cs         # STA configuration for WinForms
├── App.config                    # Test-specific settings
├── Handlers/
│   ├── TestRunnerHandlerTests.cs
│   ├── ConvertSelectedFoldersToMDTests.cs
│   └── DocXHandlerTests.cs
├── RecentFiles/
│   ├── RecentFilesManagerTests.cs
│   ├── RecentFileInfoTests.cs
│   └── RecentFilesConfigTests.cs
├── Configuration/
│   ├── UiStateConfigTests.cs
│   └── VectorStoreConfigTests.cs
└── Fakes/                        # Test doubles and mocks
    ├── FakeGitRunner.cs
    ├── FakeProcessRunner.cs
    └── InMemoryRecentFilesStore.cs
```

### **UnitTests.csproj - Complete Configuration**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <Nullable>enable</Nullable>
    <LangVersion>preview</LangVersion>
    <RootNamespace>UnitTests</RootNamespace>
  </PropertyGroup>

  <!-- ✅ LogCtx Integration -->
  <Import Project="../LogCtx/LogCtxShared/LogCtxShared.projitems" Label="Shared" />
  <Import Project="../LogCtx/NLogShared/NLogShared.projitems" Label="Shared" />

  <ItemGroup>
    <!-- ✅ Testing Framework -->
    <PackageReference Include="NUnit" Version="4.4.0" />
    <PackageReference Include="NUnit3TestAdapter" Version="5.1.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.0.0" />
    <PackageReference Include="Shouldly" Version="4.3.0" />
    
    <!-- ✅ Mocking Framework -->
    <PackageReference Include="Moq" Version="4.20.70" />
    
    <!-- ✅ LogCtx Dependencies -->
    <PackageReference Update="NLog" Version="6.0.4" />
    <PackageReference Update="NLog.Targets.Seq" Version="4.0.2" />
    <PackageReference Update="Newtonsoft.Json" Version="13.0.4" />
  </ItemGroup>

  <ItemGroup>
    <!-- ✅ Project References -->
    <ProjectReference Include="../VecTool.Core/VecTool.Core.csproj" />
    <ProjectReference Include="../VecTool.Handlers/VecTool.Handlers.csproj" />
    <ProjectReference Include="../VecTool.RecentFiles/VecTool.RecentFiles.csproj" />
    <ProjectReference Include="../VecTool.Configuration/VecTool.Configuration.csproj" />
    <ProjectReference Include="../VecTool.UI/VecTool.UI.csproj" />
  </ItemGroup>

</Project>
```

### **AssemblyAttributes.cs - WinForms Testing Setup**

```csharp
using NUnit.Framework;
using System.Threading;

// ✅ Ensure every test runs under Single-Threaded Apartment (STA) for WinForms/OLE drag-drop
[assembly: Apartment(ApartmentState.STA)]

// ✅ Keep a single test worker to avoid multiple STA workers fighting over UI resources
[assembly: LevelOfParallelism(1)]
```

### **App.config - Test Configuration**

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <appSettings>
    <!-- ✅ Test-specific values -->
    <add key="recentFilesMaxCount" value="7" />
    <add key="recentFilesRetentionDays" value="30" />
    <add key="recentFilesOutputPath" value="$(APPDATA)" />
  </appSettings>
</configuration>
```

---

## 🧪 **Basic Test Patterns**

### **Pattern 1: Simple Unit Test with LogCtx**

```csharp
using NUnit.Framework;
using Shouldly;
using LogCtxShared;
using NLogShared;

[TestFixture]
public class FileSizeFormatterTests
{
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        // ✅ Initialize LogCtx once per test fixture
        FailsafeLogger.Initialize();
    }
    
    [Test]
    public void Format_ValidByteSize_ShouldReturnFormattedString()
    {
        // Arrange
        using var testCtx = LogCtx.Set(new Props()
            .Add("TestMethod", nameof(Format_ValidByteSize_ShouldReturnFormattedString))
            .Add("TestCategory", "FileSizeFormatting")
            .Add("TestStartTime", DateTime.UtcNow));
            
        LogCtx.Logger.Info("Test execution started");
        
        const long inputBytes = 2048;
        const string expectedResult = "2 KB";
        
        testCtx.Add("InputBytes", inputBytes);
        testCtx.Add("ExpectedResult", expectedResult);
        
        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = FileSizeFormatter.Format(inputBytes);
        stopwatch.Stop();
        
        // Assert
        result.ShouldBe(expectedResult);
        
        testCtx.Add("ActualResult", result);
        testCtx.Add("ExecutionTimeMs", stopwatch.ElapsedMilliseconds);
        testCtx.Add("TestResult", "Success");
        
        LogCtx.Logger.Info("Test execution completed successfully");
    }
    
    [Test]
    public void Format_NegativeBytes_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        using var testCtx = LogCtx.Set(new Props()
            .Add("TestMethod", nameof(Format_NegativeBytes_ShouldThrowArgumentOutOfRangeException))
            .Add("TestCategory", "InputValidation"));
            
        LogCtx.Logger.Info("Exception test started");
        
        const long invalidInput = -100;
        testCtx.Add("InvalidInput", invalidInput);
        
        // Act & Assert
        var exception = Should.Throw<ArgumentOutOfRangeException>(() =>
            FileSizeFormatter.Format(invalidInput));
            
        exception.ParamName.ShouldBe("bytes");
        exception.Message.ShouldContain("Byte size cannot be negative");
        
        testCtx.Add("ExpectedException", nameof(ArgumentOutOfRangeException));
        testCtx.Add("ExceptionMessage", exception.Message);
        
        LogCtx.Logger.Info("Exception test completed successfully");
    }
}
```

### **Pattern 2: Test Data Generation with TestCase**

```csharp
[TestFixture]
public class MimeTypeProviderTests
{
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        FailsafeLogger.Initialize();
    }
    
    [TestCase(".cs", "csharp")]
    [TestCase(".csproj", "msbuild")]
    [TestCase(".feature", "gherkin")]
    [TestCase(".unknown", "")]
    [TestCase(".txt", "")]
    [TestCase(null, "")]
    public void GetMdTag_ValidAndInvalidExtensions_ReturnsCorrectMdTag(string extension, string expectedMdTag)
    {
        using var testCtx = LogCtx.Set(new Props()
            .Add("TestMethod", nameof(GetMdTag_ValidAndInvalidExtensions_ReturnsCorrectMdTag))
            .Add("InputExtension", extension)
            .Add("ExpectedMdTag", expectedMdTag));
            
        LogCtx.Logger.Info("TestCase execution started");
        
        var result = MimeTypeProvider.GetMdTag(extension);
        
        result.ShouldBe(expectedMdTag);
        
        testCtx.Add("ActualResult", result);
        LogCtx.Logger.Info("TestCase execution completed");
    }
    
    [TestCase("", "application/octet-stream")]
    [TestCase(null, "application/octet-stream")]
    [TestCase(".verylongextensionthatshouldbehandledproperly", "application/octet-stream")]
    [TestCase(".json", "application/json")]
    public void GetMimeType_InvalidOrEdgeCases_ReturnsCorrectMimeType(string? extension, string expectedMimeType)
    {
        using var testCtx = LogCtx.Set(new Props()
            .Add("TestMethod", nameof(GetMimeType_InvalidOrEdgeCases_ReturnsCorrectMimeType))
            .Add("InputExtension", extension)
            .Add("ExpectedMimeType", expectedMimeType));
            
        var result = MimeTypeProvider.GetMimeType(extension);
        result.ShouldBe(expectedMimeType);
    }
}
```

---

## 🔧 **Advanced Testing Patterns**

### **Pattern 3: File System Integration Tests**

```csharp
[TestFixture]
public class ConvertSelectedFoldersToMDTests : DocTestBase
{
    private string testRootPath = default!;
    private string outputMarkdownPath = default!;
    
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        FailsafeLogger.Initialize();
        
        using var fixtureCtx = LogCtx.Set(new Props()
            .Add("TestFixture", nameof(ConvertSelectedFoldersToMDTests))
            .Add("SetupTime", DateTime.UtcNow));
            
        LogCtx.Logger.Info("Test fixture setup initiated");
    }
    
    [SetUp]
    public void Setup()
    {
        using var setupCtx = LogCtx.Set(new Props()
            .Add("Operation", "TestSetup")
            .Add("TestId", Guid.NewGuid()));
            
        testRootPath = Path.Combine(Path.GetTempPath(), "ConvertSelectedFoldersToMDTests");
        Directory.CreateDirectory(testRootPath);
        
        outputMarkdownPath = Path.Combine(testRootPath, "output.md");
        
        setupCtx.Add("TestRootPath", testRootPath);
        setupCtx.Add("OutputMarkdownPath", outputMarkdownPath);
        
        LogCtx.Logger.Debug("Test setup completed");
    }
    
    [Test]
    public void ExportSelectedFoldersToMarkdown_MultipleFolders_ShouldIncludeAllInMarkdown()
    {
        // Arrange
        using var testCtx = LogCtx.Set(new Props()
            .Add("TestMethod", nameof(ExportSelectedFoldersToMarkdown_MultipleFolders_ShouldIncludeAllInMarkdown))
            .Add("TestCategory", "MarkdownExport")
            .Add("TestStartTime", DateTime.UtcNow));
        
        LogCtx.Logger.Info("Integration test execution started");
        
        string folder1 = Path.Combine(testRootPath, "MarkdownFolder1Name");
        string folder2 = Path.Combine(testRootPath, "MarkdownFolder2Name");
        Directory.CreateDirectory(folder1);
        Directory.CreateDirectory(folder2);
        
        string textFilePath1 = Path.Combine(folder1, "Markdown1FileName");
        string textFilePath2 = Path.Combine(folder2, "Markdown2FileName");
        
        File.WriteAllText(textFilePath1, "ContentOfMarkdownFile1");
        File.WriteAllText(textFilePath2, "ContentOfMarkdownFile2");
        
        testCtx.Add("InputFolders", 2);
        testCtx.Add("InputFiles", 2);
        
        List<string> folderPaths = new List<string> { folder1, folder2 };
        
        // Act
        var stopwatch = Stopwatch.StartNew();
        
        var mdHandler = new MDHandler(null, null);
        mdHandler.ExportSelectedFolders(folderPaths, outputMarkdownPath, new VectorStoreConfig());
        
        stopwatch.Stop();
        
        // Assert
        File.Exists(outputMarkdownPath).ShouldBeTrue();
        
        string markdownContent = File.ReadAllText(outputMarkdownPath);
        
        // ✅ Multiple specific assertions with context
        markdownContent.ShouldContain("Folder MarkdownFolder1Name");
        markdownContent.ShouldContain("File Markdown1FileName");
        markdownContent.ShouldContain("ContentOfMarkdownFile1");
        markdownContent.ShouldContain("Folder MarkdownFolder2Name");
        markdownContent.ShouldContain("File Markdown2FileName");
        markdownContent.ShouldContain("ContentOfMarkdownFile2");
        
        testCtx.Add("ExecutionTimeMs", stopwatch.ElapsedMilliseconds);
        testCtx.Add("OutputSizeBytes", markdownContent.Length);
        testCtx.Add("AssertionsPassed", 6);
        testCtx.Add("TestResult", "Success");
        
        LogCtx.Logger.Info("Integration test execution completed successfully");
    }
    
    [TearDown]
    public void Cleanup()
    {
        using var cleanupCtx = LogCtx.Set(new Props()
            .Add("Operation", "TestCleanup")
            .Add("TestRootPath", testRootPath));
        
        try
        {
            if (Directory.Exists(testRootPath))
            {
                Directory.Delete(testRootPath, true);
                cleanupCtx.Add("CleanupSuccess", true);
                LogCtx.Logger.Debug("Test cleanup completed successfully");
            }
        }
        catch (Exception ex)
        {
            cleanupCtx.Add("CleanupSuccess", false);
            cleanupCtx.Add("CleanupError", ex.GetType().Name);
            LogCtx.Logger.Warn(ex, "Test cleanup encountered issues - continuing");
        }
    }
}
```

### **Pattern 4: Configuration Testing with JSON Round-trips**

```csharp
[TestFixture]
public sealed class UiStateConfigJsonTests
{
    private string tempDir = default!;
    
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        FailsafeLogger.Initialize();
    }
    
    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"vectool-ui-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
    }
    
    [TearDown]
    public void TearDown()
    {
        try
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
        catch
        {
            // Swallow cleanup exceptions in tests
        }
    }
    
    [Test]
    public void LoadWithoutFile_ReturnsDefaults()
    {
        using var testCtx = LogCtx.Set(new Props()
            .Add("TestMethod", nameof(LoadWithoutFile_ReturnsDefaults))
            .Add("TempDir", tempDir));
            
        LogCtx.Logger.Info("Configuration default loading test started");
        
        var state = UiStateConfig.Load(tempDir);
        
        state.ShouldNotBeNull();
        state.RecentFilesColumnWidths.Count.ShouldBe(0);
        state.RecentFilesRowHeightScale.ShouldBeNull();
        
        testCtx.Add("DefaultsLoaded", true);
        LogCtx.Logger.Info("Configuration defaults validated");
    }
    
    [Test]
    public void SaveThenLoad_RoundTripsValues()
    {
        using var testCtx = LogCtx.Set(new Props()
            .Add("TestMethod", nameof(SaveThenLoad_RoundTripsValues))
            .Add("TempDir", tempDir));
            
        LogCtx.Logger.Info("Configuration round-trip test started");
        
        // Arrange
        var before = new UiStateConfig.UiState
        {
            RecentFilesRowHeightScale = 1.25
        };
        before.RecentFilesColumnWidths["File"] = 420;
        before.RecentFilesColumnWidths["Type"] = 120;
        
        testCtx.Add("InputRowHeightScale", before.RecentFilesRowHeightScale);
        testCtx.Add("InputColumnCount", before.RecentFilesColumnWidths.Count);
        
        // Act
        UiStateConfig.Save(before, tempDir);
        var after = UiStateConfig.Load(tempDir);
        
        // Assert
        after.RecentFilesRowHeightScale.ShouldBe(1.25);
        after.RecentFilesColumnWidths.ShouldContainKeyAndValue("File", 420);
        after.RecentFilesColumnWidths.ShouldContainKeyAndValue("Type", 120);
        
        testCtx.Add("RoundTripSuccess", true);
        testCtx.Add("OutputColumnCount", after.RecentFilesColumnWidths.Count);
        
        LogCtx.Logger.Info("Configuration round-trip test completed successfully");
    }
}
```

---

## 🎭 **Mocking and Test Doubles**

### **Pattern 5: Test Doubles with Interface Segregation**

```csharp
// ✅ Fake implementations for testing
public class FakeGitRunner : IGitRunner
{
    private readonly string _branch;
    
    public FakeGitRunner(string branch = "main")
    {
        _branch = branch;
    }
    
    public Task<string> GetCurrentBranchAsync(string workingDirectory)
    {
        using var ctx = LogCtx.Set(new Props()
            .Add("Operation", "FakeGetCurrentBranch")
            .Add("WorkingDirectory", workingDirectory)
            .Add("FakeBranch", _branch));
            
        LogCtx.Logger.Debug("Fake git branch operation");
        
        return Task.FromResult(_branch);
    }
    
    public Task<string> GetGitChangesAsync(string workingDirectory)
    {
        using var ctx = LogCtx.Set(new Props()
            .Add("Operation", "FakeGetGitChanges")
            .Add("WorkingDirectory", workingDirectory));
            
        LogCtx.Logger.Debug("Fake git changes operation");
        
        return Task.FromResult("M modified-file.cs\nA new-file.cs");
    }
}

public class FakeProcessRunner : IProcessRunner
{
    private readonly int _exitCode;
    private readonly string _standardOutput;
    private readonly string _standardError;
    
    public FakeProcessRunner(int exitCode = 0, string standardOutput = "", string standardError = "")
    {
        _exitCode = exitCode;
        _standardOutput = standardOutput;
        _standardError = standardError;
    }
    
    public Task<ProcessResult> RunAsync(string executable, string arguments)
    {
        using var ctx = LogCtx.Set(new Props()
            .Add("Operation", "FakeProcessRun")
            .Add("Executable", executable)
            .Add("Arguments", arguments)
            .Add("FakeExitCode", _exitCode));
            
        LogCtx.Logger.Debug("Fake process execution");
        
        var result = new ProcessResult
        {
            ExitCode = _exitCode,
            StandardOutput = _standardOutput,
            StandardError = _standardError
        };
        
        return Task.FromResult(result);
    }
}
```

### **Pattern 6: Testing with Dependency Injection**

```csharp
[TestFixture]
public class TestRunnerHandlerTests
{
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        FailsafeLogger.Initialize();
    }
    
    [Test]
    public void Constructor_ValidDependencies_ShouldSucceed()
    {
        using var testCtx = LogCtx.Set(new Props()
            .Add("TestMethod", nameof(Constructor_ValidDependencies_ShouldSucceed))
            .Add("TestCategory", "DependencyInjection"));
            
        LogCtx.Logger.Info("Dependency injection test started");
        
        // Arrange
        IGitRunner gitRunner = new FakeGitRunner("main");
        IProcessRunner processRunner = new FakeProcessRunner();
        IUserInterface ui = new FakeUserInterface();
        IRecentFilesManager recentFiles = new NoopRecentFilesManager();
        
        testCtx.Add("DependencyCount", 4);
        
        // Act & Assert
        var handler = new TestRunnerHandler(gitRunner, processRunner, ui, recentFiles);
        
        handler.ShouldNotBeNull();
        
        testCtx.Add("ConstructorSuccess", true);
        LogCtx.Logger.Info("Dependency injection test completed");
    }
    
    [Test]
    public async Task RunTestsAsync_NonZeroExitCode_ReturnsNull()
    {
        using var testCtx = LogCtx.Set(new Props()
            .Add("TestMethod", nameof(RunTestsAsync_NonZeroExitCode_ReturnsNull))
            .Add("TestCategory", "ErrorHandling"));
            
        LogCtx.Logger.Info("Error handling test started");
        
        // Arrange
        IGitRunner git = new FakeGitRunner("dev");
        IProcessRunner proc = new FakeProcessRunner(exitCode: 2, stdout: "", stderr: "Test failed");
        var handler = new TestRunnerHandler(git, proc, ui: null, recentFilesManager: null);
        
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        
        var sln = Path.Combine(tempDir, "VecTool.sln");
        await File.WriteAllTextAsync(sln, "Microsoft Visual Studio Solution File, Format Version 12.00");
        
        testCtx.Add("TempDir", tempDir);
        testCtx.Add("ExpectedExitCode", 2);
        
        try
        {
            // Act
            var stopwatch = Stopwatch.StartNew();
            var result = await handler.RunTestsAsync(sln, "Debug", Array.Empty<string>(), CancellationToken.None);
            stopwatch.Stop();
            
            // Assert
            result.ShouldBeNull();
            
            testCtx.Add("ExecutionTimeMs", stopwatch.ElapsedMilliseconds);
            testCtx.Add("ResultIsNull", true);
            
            LogCtx.Logger.Info("Error handling test completed successfully");
        }
        finally
        {
            TryDeleteDir(tempDir);
        }
    }
    
    private static void TryDeleteDir(string dir)
    {
        try
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
        catch
        {
            // Swallow cleanup exceptions in tests
        }
    }
}
```

---

## 📊 **Collection and State Testing**

### **Pattern 7: Recent Files Manager Testing**

```csharp
[TestFixture]
public class RecentFilesManagerTests
{
    private static RecentFilesConfig TestConfig(int maxCount = 10, int retentionDays = 30) =>
        new(maxCount, retentionDays, @"C:\");
    
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        FailsafeLogger.Initialize();
    }
    
    [Test]
    public void RegisterFile_ShouldAddNewFile()
    {
        using var testCtx = LogCtx.Set(new Props()
            .Add("TestMethod", nameof(RegisterFile_ShouldAddNewFile))
            .Add("TestCategory", "RecentFilesManagement"));
            
        LogCtx.Logger.Info("Recent files registration test started");
        
        // Arrange
        var manager = new RecentFilesManager(TestConfig(), new InMemoryRecentFilesStore());
        
        testCtx.Add("InitialFileCount", 0);
        
        // Act
        manager.RegisterGeneratedFile(@"C:\test.docx", RecentFileType.Docx, new[] { @"C:\" });
        var files = manager.GetRecentFiles();
        
        // Assert
        files.Count.ShouldBe(1);
        files[0].FileName.ShouldBe("test.docx");
        files[0].FileType.ShouldBe(RecentFileType.Docx);
        
        testCtx.Add("FinalFileCount", files.Count);
        testCtx.Add("RegisteredFileName", files[0].FileName);
        
        LogCtx.Logger.Info("Recent files registration test completed");
    }
    
    [Test]
    public void RegisterFile_WhenFileExists_ShouldUpdateTimestamp()
    {
        using var testCtx = LogCtx.Set(new Props()
            .Add("TestMethod", nameof(RegisterFile_WhenFileExists_ShouldUpdateTimestamp))
            .Add("TestCategory", "TimestampUpdate"));
            
        LogCtx.Logger.Info("Timestamp update test started");
        
        // Arrange
        var manager = new RecentFilesManager(TestConfig(), new InMemoryRecentFilesStore());
        var now = DateTime.Now;
        
        // Register file initially
        manager.RegisterGeneratedFile(@"C:\test.docx", RecentFileType.Docx, new[] { @"C:\" }, 1, now.AddMinutes(-10));
        
        testCtx.Add("InitialTimestamp", now.AddMinutes(-10));
        
        // Act - Register same file with new timestamp
        manager.RegisterGeneratedFile(@"C:\test.docx", RecentFileType.Docx, new[] { @"C:\" }, 1, now);
        var files = manager.GetRecentFiles();
        
        // Assert
        files.Count.ShouldBe(1);
        files[0].GeneratedAt.ShouldBe(now);
        
        testCtx.Add("UpdatedTimestamp", files[0].GeneratedAt);
        testCtx.Add("TimestampUpdateSuccess", true);
        
        LogCtx.Logger.Info("Timestamp update test completed");
    }
    
    [Test]
    public void MaxFileLimit_ShouldRemoveOldest()
    {
        using var testCtx = LogCtx.Set(new Props()
            .Add("TestMethod", nameof(MaxFileLimit_ShouldRemoveOldest))
            .Add("TestCategory", "PolicyEnforcement"));
            
        LogCtx.Logger.Info("Policy enforcement test started");
        
        // Arrange
        var manager = new RecentFilesManager(TestConfig(maxCount: 2), new InMemoryRecentFilesStore());
        
        testCtx.Add("MaxCount", 2);
        
        // Act - Add 3 files
        manager.RegisterGeneratedFile(@"C:\oldest.txt", RecentFileType.Unknown, Array.Empty<string>(), 1, DateTime.Now.AddMinutes(-3));
        manager.RegisterGeneratedFile(@"C:\middle.txt", RecentFileType.Unknown, Array.Empty<string>(), 1, DateTime.Now.AddMinutes(-2));
        manager.RegisterGeneratedFile(@"C:\newest.txt", RecentFileType.Unknown, Array.Empty<string>(), 1, DateTime.Now.AddMinutes(-1));
        
        var files = manager.GetRecentFiles();
        
        // Assert
        files.Count.ShouldBe(2);
        files.Any(f => f.FileName == "oldest.txt").ShouldBeFalse();
        files.Any(f => f.FileName == "newest.txt").ShouldBeTrue();
        
        testCtx.Add("FilesAfterPolicyEnforcement", files.Count);
        testCtx.Add("OldestFileRemoved", true);
        
        LogCtx.Logger.Info("Policy enforcement test completed");
    }
}
```

---

## 🎪 **UI and Integration Testing**

### **Pattern 8: UI Component Testing**

```csharp
[TestFixture]
public sealed class RecentFilesPanelDragDropTests
{
    private RecentFilesPanel panel = null!;
    
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        FailsafeLogger.Initialize();
    }
    
    [SetUp]
    public void SetUp()
    {
        // ✅ Ensure we instantiate the actual UI control
        panel = new RecentFilesPanel();
    }
    
    [TearDown]
    public void TearDown()
    {
        panel?.Dispose();
    }
    
    [Test]
    public void DragDrop_ShouldAcceptKnownFileExtensions()
    {
        using var testCtx = LogCtx.Set(new Props()
            .Add("TestMethod", nameof(DragDrop_ShouldAcceptKnownFileExtensions))
            .Add("TestCategory", "UIDragDrop"));
            
        LogCtx.Logger.Info("UI drag-drop test started");
        
        // Example: simulate drag-drop of different file types
        var droppedFiles = new[] { "readme.md", "CHANGELOG.md", "results.trx", "report.pdf", "notes.txt" };
        
        testCtx.Add("DroppedFileCount", droppedFiles.Length);
        
        // Hypothetical: panel filters to only show supported recent file entries
        var accepted = FilterSupportedFiles(droppedFiles);
        
        accepted.ShouldContain("readme.md");
        accepted.ShouldContain("CHANGELOG.md");
        // Depending on design, TRX or PDF may or may not be supported in UI list
        accepted.ShouldNotContain("notes.txt");
        
        testCtx.Add("AcceptedFileCount", accepted.Count());
        
        LogCtx.Logger.Info("UI drag-drop test completed");
    }
    
    [Test]
    public void DragDrop_ShouldMapToDomainEnumWhenBindingOccurs()
    {
        using var testCtx = LogCtx.Set(new Props()
            .Add("TestMethod", nameof(DragDrop_ShouldMapToDomainEnumWhenBindingOccurs))
            .Add("TestCategory", "EnumMapping"));
            
        LogCtx.Logger.Info("Enum mapping test started");
        
        // Demonstrate explicit mapping where UI and Domain enums diverge
        var uiType = RecentFileType.Md;
        var domainType = MapToDomainType(uiType);
        
        domainType.ShouldBe(VecTool.RecentFiles.RecentFileType.Md);
        
        testCtx.Add("MappingSuccess", true);
        
        LogCtx.Logger.Info("Enum mapping test completed");
    }
    
    // Helper methods for UI testing
    private static IEnumerable<string> FilterSupportedFiles(string[] files) =>
        files.Where(f => Path.GetExtension(f) is ".md" or ".trx" or ".pdf");
        
    private static VecTool.RecentFiles.RecentFileType MapToDomainType(RecentFileType uiType) =>
        uiType switch
        {
            RecentFileType.Md => VecTool.RecentFiles.RecentFileType.Md,
            RecentFileType.Docx => VecTool.RecentFiles.RecentFileType.Docx,
            _ => VecTool.RecentFiles.RecentFileType.Unknown
        };
}
```

---

## 📈 **Performance and Load Testing**

### **Pattern 9: Performance Testing with LogCtx**

```csharp
[TestFixture]
public class PerformanceTests
{
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        FailsafeLogger.Initialize();
    }
    
    [Test]
    public void ProcessLargeBatch_ShouldCompleteWithinTimeLimit()
    {
        using var testCtx = LogCtx.Set(new Props()
            .Add("TestMethod", nameof(ProcessLargeBatch_ShouldCompleteWithinTimeLimit))
            .Add("TestCategory", "Performance")
            .Add("ExpectedMaxDurationMs", 5000));
            
        LogCtx.Logger.Info("Performance test started");
        
        // Arrange
        const int batchSize = 10000;
        var items = Enumerable.Range(1, batchSize).Select(i => $"item-{i}").ToList();
        var processor = new BatchProcessor();
        
        testCtx.Add("BatchSize", batchSize);
        testCtx.Add("TestStartTime", DateTime.UtcNow);
        
        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = processor.ProcessBatch(items);
        stopwatch.Stop();
        
        // Assert
        result.ShouldNotBeNull();
        result.ProcessedCount.ShouldBe(batchSize);
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(5000); // 5 second limit
        
        testCtx.Add("ActualDurationMs", stopwatch.ElapsedMilliseconds);
        testCtx.Add("ItemsPerSecond", (double)batchSize / stopwatch.Elapsed.TotalSeconds);
        testCtx.Add("PerformanceTestPassed", true);
        
        LogCtx.Logger.Info("Performance test completed successfully");
    }
}
```

---

## 📋 **Version Consistency Testing**

### **Pattern 10: Assembly Version Validation**

```csharp
[TestFixture]
public sealed class VersionConsistencyTests
{
    private static readonly string SolutionRoot = GetSolutionRoot();
    
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        FailsafeLogger.Initialize();
    }
    
    [Test]
    public void AllCsprojFiles_ShouldHaveMajorVersionAndPlanId()
    {
        using var testCtx = LogCtx.Set(new Props()
            .Add("TestMethod", nameof(AllCsprojFiles_ShouldHaveMajorVersionAndPlanId))
            .Add("TestCategory", "VersionConsistency")
            .Add("SolutionRoot", SolutionRoot));
            
        LogCtx.Logger.Info("Version consistency validation started");
        
        var csprojFiles = Directory.GetFiles(SolutionRoot, "*.csproj", SearchOption.AllDirectories);
        csprojFiles.Length.ShouldBeGreaterThan(0, "No .csproj files found in solution");
        
        testCtx.Add("CsprojFileCount", csprojFiles.Length);
        
        var violations = new List<string>();
        
        foreach (var file in csprojFiles)
        {
            var doc = XDocument.Load(file);
            var ns = doc.Root?.Name.Namespace ?? XNamespace.None;
            
            var major = doc.Descendants(ns + "MajorVersion").FirstOrDefault()?.Value;
            var planId = doc.Descendants(ns + "PlanId").FirstOrDefault()?.Value;
            
            if (string.IsNullOrWhiteSpace(major))
                violations.Add($"{Path.GetFileName(file)}: Missing MajorVersion");
                
            if (string.IsNullOrWhiteSpace(planId))
                violations.Add($"{Path.GetFileName(file)}: Missing PlanId");
        }
        
        if (violations.Any())
        {
            var message = new StringBuilder("Version property violations detected:\n");
            violations.ForEach(v => message.AppendLine($"  - {v}"));
            Assert.Fail(message.ToString());
        }
        
        testCtx.Add("ViolationCount", violations.Count);
        testCtx.Add("ValidationPassed", true);
        
        LogCtx.Logger.Info("Version consistency validation completed");
    }
    
    [Test]
    public void AllCsprojFiles_ShouldShareSameMajorVersionAndPlanId()
    {
        using var testCtx = LogCtx.Set(new Props()
            .Add("TestMethod", nameof(AllCsprojFiles_ShouldShareSameMajorVersionAndPlanId))
            .Add("TestCategory", "VersionUniformity"));
            
        LogCtx.Logger.Info("Version uniformity check started");
        
        var csprojFiles = Directory.GetFiles(SolutionRoot, "*.csproj", SearchOption.AllDirectories);
        var versions = new Dictionary<string, (string major, string planId)>();
        
        foreach (var file in csprojFiles)
        {
            var doc = XDocument.Load(file);
            var ns = doc.Root?.Name.Namespace ?? XNamespace.None;
            
            var major = doc.Descendants(ns + "MajorVersion").FirstOrDefault()?.Value ?? "0";
            var planId = doc.Descendants(ns + "PlanId").FirstOrDefault()?.Value ?? "0";
            
            versions[Path.GetFileName(file)] = (major, planId);
        }
        
        var distinctMajors = versions.Select(v => v.Value.major).Distinct().ToList();
        var distinctPlanIds = versions.Select(v => v.Value.planId).Distinct().ToList();
        
        distinctMajors.Count.ShouldBe(1, $"MajorVersion inconsistency: {string.Join(", ", distinctMajors)}");
        distinctPlanIds.Count.ShouldBe(1, $"PlanId inconsistency: {string.Join(", ", distinctPlanIds)}");
        
        testCtx.Add("UniqueMajorVersions", distinctMajors.Count);
        testCtx.Add("UniquePlanIds", distinctPlanIds.Count);
        testCtx.Add("ConsistencyCheckPassed", true);
        
        LogCtx.Logger.Info("Version uniformity check completed");
    }
    
    private static string GetSolutionRoot()
    {
        var dir = TestContext.CurrentContext.TestDirectory;
        while (dir != null && !Directory.GetFiles(dir, "*.sln").Any())
        {
            dir = Directory.GetParent(dir)?.FullName;
        }
        
        dir.ShouldNotBeNull("Solution root not found");
        return dir!;
    }
}
```

---

## 🎯 **Test Organization Best Practices**

### **Test Categories and Organization**

```csharp
// ✅ Test categories for filtering and organization
[Category("UnitTest")]
[Category("FileSystem")]
public class FileProcessorTests { }

[Category("IntegrationTest")]
[Category("UI")]  
public class RecentFilesPanelTests { }

[Category("Performance")]
[Category("LoadTest")]
public class BatchProcessingTests { }
```

### **Test Data Management**

```csharp
// ✅ Test data helper class
public static class TestDataHelper
{
    public static string CreateTempFile(string extension, string content)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.{extension}");
        File.WriteAllText(tempFile, content);
        return tempFile;
    }
    
    public static string CreateTempDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        return tempDir;
    }
    
    public static RecentFilesConfig CreateTestConfig(int maxCount = 10, int retentionDays = 30) =>
        new(maxCount, retentionDays, CreateTempDirectory());
}

// ✅ Base test class for common setup
public abstract class DocTestBase
{
    protected string testRootPath = default!;
    protected string outputDocxPath = default!;
    
    [OneTimeSetUp]
    public void BaseOneTimeSetup()
    {
        FailsafeLogger.Initialize();
    }
}
```

---

## 🔍 **Testing Checklist**

### **Essential LogCtx Test Patterns**
- [ ] **FailsafeLogger.Initialize()**: Called once in `[OneTimeSetUp]`
- [ ] **Test context per method**: Each test method has `using var testCtx = LogCtx.Set(...)`
- [ ] **Meaningful test properties**: TestMethod, TestCategory, start/end times
- [ ] **Performance tracking**: Execution times, item counts, success metrics
- [ ] **Error context enrichment**: Exception details logged with context

### **NUnit + Shouldly Standards**
- [ ] **Test naming**: `MethodName_Scenario_ExpectedBehavior` pattern
- [ ] **Shouldly assertions**: All assertions use Shouldly syntax with clear messages
- [ ] **Proper setup/teardown**: Resources created and disposed properly
- [ ] **Test categories**: Tests categorized for filtering (UnitTest, IntegrationTest, Performance)

### **File System Testing**
- [ ] **Temp directories**: Use `Path.GetTempPath()` with unique identifiers
- [ ] **Cleanup in teardown**: Always clean up temp files/directories
- [ ] **Defensive cleanup**: Swallow cleanup exceptions in tests
- [ ] **Path handling**: Use `Path.Combine()` for cross-platform compatibility

### **Dependency Injection Testing**
- [ ] **Interface-based testing**: Test against interfaces, not concrete types
- [ ] **Fake implementations**: Use fakes instead of complex mocks for simple scenarios
- [ ] **Constructor validation**: Test null argument validation
- [ ] **Multiple scenarios**: Test success, failure, and edge cases

### **Integration Testing**
- [ ] **Real file operations**: Actually create/read/write files when testing file handlers
- [ ] **Configuration testing**: Test with real configuration files and JSON serialization
- [ ] **Error scenarios**: Test file not found, access denied, corrupted data
- [ ] **Performance boundaries**: Include timing assertions for performance-critical operations

---

**Next Steps**: See [Troubleshooting.md](Troubleshooting.md) for common testing issues and solutions! 🚀