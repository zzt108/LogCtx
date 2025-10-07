# LogCtx Usage Patterns & Examples
*Real-world patterns from VecTool production codebase*

## 🎯 **VecTool: Real-World LogCtx Implementation**

VecTool is a **7-project modular architecture** with centralized LogCtx logging, providing real-world examples of structured logging in production.

### **VecTool Architecture Overview**
```
VecTool Solution/
├── Vectool.UI/          # Main WinForms interface
├── Core/                # Business logic & Git operations  
├── Handlers/            # Feature handlers (Markdown, Git, Tests)
├── RecentFiles/         # Recent Files manager with drag-drop
├── Configuration/       # Settings stores & app.config abstraction
├── Constants/           # Centralized XML tag constants
├── Utils/               # Utilities (MIME detection, file helpers)
└── LogCtx/              # Git submodule - Structured logging
    ├── LogCtxShared/    # Core interfaces
    ├── NLogShared/      # NLog implementation (PRIMARY)
    └── SeriLogShared/   # Serilog implementation (SECONDARY)
```

---

## 🚀 **Pattern 1: Application Initialization**

### **Program.cs - Failsafe Startup Logging**
```csharp
using NLogShared;
using LogCtxShared;

static class Program
{
    [STAThread]
    static void Main()
    {
        // ✅ VecTool Pattern: Failsafe initialization that never throws
        FailsafeLogger.Initialize("NLog.config");
        
        // ✅ Application startup context
        using var startupCtx = LogCtx.Set(new Props()
            .Add("Application", "VecTool")
            .Add("Version", "4.25.1007")
            .Add("Environment", "Development")
            .Add("ProcessId", Environment.ProcessId)
            .Add("StartupTime", DateTime.UtcNow));
            
        LogCtx.Logger.Info("VecTool application startup initiated");
        
        try
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            startupCtx.Add("UIInitialized", true);
            LogCtx.Logger.Info("UI subsystem initialized");
            
            Application.Run(new MainForm());
            
            LogCtx.Logger.Info("VecTool application shutdown gracefully");
        }
        catch (Exception ex)
        {
            startupCtx.Add("FatalError", ex.GetType().Name);
            startupCtx.Add("ErrorMessage", ex.Message);
            LogCtx.Logger.Fatal(ex, "VecTool application failed to start");
            throw;
        }
    }
}
```

### **App.config - VecTool Configuration**
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <appSettings>
    <!-- Recent Files Configuration -->
    <add key="recentFilesMaxCount" value="200" />
    <add key="recentFilesRetentionDays" value="30" />
    <add key="recentFilesOutputPath" value="Generated" />
    
    <!-- Vector Store Configuration -->
    <add key="vectorStoreFoldersPath" value="Config/vectorStoreFolders.json" />
    <add key="excludedFiles" value="*.tmp,*.cache,*.log" />
    <add key="excludedFolders" value="bin,obj,node_modules,.git" />
  </appSettings>
</configuration>
```

---

## 🔧 **Pattern 2: Configuration Management with Validation**

### **RecentFilesConfig.cs - Validated Configuration Loading**
```csharp
using LogCtxShared;
using NLogShared;

public sealed class RecentFilesConfig
{
    private static readonly CtxLogger log = new();
    
    public static RecentFilesConfig FromAppConfig(IAppSettingsReader? reader = null)
    {
        using var ctx = LogCtx.Set(new Props()
            .Add("Operation", "LoadRecentFilesConfig")
            .Add("ConfigSource", reader?.GetType().Name ?? "DefaultAppSettings"));
            
        LogCtx.Logger.Info("Loading Recent Files configuration");
        
        try
        {
            reader ??= new ConfigurationManagerAppSettingsReader();
            
            int maxCount = ParseIntOrDefault(reader.Get(CONFIGKEY_MAXCOUNT), DefaultMaxCount);
            int retention = ParseIntOrDefault(reader.Get(CONFIGKEY_RETENTIONDAYS), DefaultRetentionDays);
            string output = reader.Get(CONFIGKEY_OUTPUTPATH) ?? DefaultOutputPath;
            
            ctx.Add("MaxCount", maxCount);
            ctx.Add("RetentionDays", retention);
            ctx.Add("OutputPath", output);
            ctx.Add("ConfigurationValid", true);
            
            var config = new RecentFilesConfig(maxCount, retention, output);
            
            LogCtx.Logger.Info("Recent Files configuration loaded successfully");
            return config;
        }
        catch (ArgumentOutOfRangeException ex)
        {
            ctx.Add("ValidationError", ex.ParamName);
            ctx.Add("ErrorType", "ConfigurationValidation");
            LogCtx.Logger.Error(ex, "Invalid configuration values detected");
            throw;
        }
        catch (Exception ex)
        {
            ctx.Add("ErrorType", ex.GetType().Name);
            LogCtx.Logger.Error(ex, "Failed to load Recent Files configuration");
            throw;
        }
    }
    
    private static int ParseIntOrDefault(string? value, int defaultValue)
    {
        using var parseCtx = LogCtx.Set(new Props()
            .Add("Operation", "ParseConfigValue")
            .Add("RawValue", value)
            .Add("DefaultValue", defaultValue));
        
        if (int.TryParse(value, out int parsed))
        {
            parseCtx.Add("ParsedValue", parsed);
            parseCtx.Add("ParseSuccess", true);
            LogCtx.Logger.Debug("Configuration value parsed successfully");
            return parsed;
        }
        
        parseCtx.Add("ParseSuccess", false);
        parseCtx.Add("UsingDefault", true);
        LogCtx.Logger.Warn("Failed to parse configuration value, using default");
        return defaultValue;
    }
}
```

---

## 📁 **Pattern 3: File System Operations with Context**

### **VectorStoreConfig.cs - File Operations with Rich Context**
```csharp
using LogCtxShared;
using NLogShared;

public class VectorStoreConfig
{
    private static readonly CtxLogger log = new();
    
    public static Dictionary<string, VectorStoreConfig> LoadAll(string? configPath = null)
    {
        using var ctx = LogCtx.Set(new Props()
            .Add("Operation", "LoadAllVectorStoreConfigs")
            .Add("ConfigPath", configPath)
            .Add("StartTime", DateTime.UtcNow));
            
        string vectorStoreFoldersPath = configPath ?? 
            ConfigurationManager.AppSettings["vectorStoreFoldersPath"] ?? 
            "Config/vectorStoreFolders.json";
        
        ctx.Add("ResolvedPath", vectorStoreFoldersPath);
        LogCtx.Logger.Info("Loading vector store configurations");
        
        var configs = new Dictionary<string, VectorStoreConfig>();
        
        if (File.Exists(vectorStoreFoldersPath))
        {
            try
            {
                var fileInfo = new FileInfo(vectorStoreFoldersPath);
                ctx.Add("FileSize", fileInfo.Length);
                ctx.Add("LastModified", fileInfo.LastWriteTime);
                
                string json = File.ReadAllText(vectorStoreFoldersPath);
                ctx.Add("JsonLength", json.Length);
                
                var deserializedConfigs = JsonSerializer.Deserialize<Dictionary<string, VectorStoreConfig>>(json);
                
                if (deserializedConfigs != null)
                {
                    configs = deserializedConfigs;
                    ctx.Add("ConfigCount", configs.Count);
                    ctx.Add("LoadSuccess", true);
                    
                    LogCtx.Logger.Info("Vector store configurations loaded successfully");
                }
                else
                {
                    ctx.Add("LoadSuccess", false);
                    ctx.Add("ErrorReason", "DeserializationReturnedNull");
                    LogCtx.Logger.Warn("Deserialization returned null, using empty configuration");
                }
            }
            catch (JsonException ex)
            {
                ctx.Add("ErrorType", "JsonException");
                ctx.Add("JsonError", ex.Message);
                LogCtx.Logger.Error(ex, "Invalid JSON format in configuration file");
                // Return empty configs rather than throwing
            }
            catch (IOException ex)
            {
                ctx.Add("ErrorType", "IOException");
                ctx.Add("IOError", ex.Message);
                LogCtx.Logger.Error(ex, "File access error loading configuration");
            }
            catch (Exception ex)
            {
                ctx.Add("ErrorType", ex.GetType().Name);
                LogCtx.Logger.Error(ex, "Unexpected error loading vector store configurations");
            }
        }
        else
        {
            ctx.Add("FileExists", false);
            ctx.Add("UsingDefaults", true);
            LogCtx.Logger.Info("Configuration file not found, using default empty configuration");
        }
        
        return configs;
    }
    
    public bool IsFileExcluded(string fileName)
    {
        using var ctx = LogCtx.Set(new Props()
            .Add("Operation", "CheckFileExclusion")
            .Add("FileName", fileName)
            .Add("PatternCount", ExcludedFiles.Count));
        
        foreach (var pattern in ExcludedFiles)
        {
            string regexPattern = Regex.Escape(pattern).Replace("\\*", ".*");
            
            if (Regex.IsMatch(fileName, regexPattern, RegexOptions.IgnoreCase))
            {
                ctx.Add("ExcludedByPattern", pattern);
                ctx.Add("IsExcluded", true);
                LogCtx.Logger.Debug("File excluded by pattern");
                return true;
            }
        }
        
        ctx.Add("IsExcluded", false);
        LogCtx.Logger.Debug("File not excluded by any pattern");
        return false;
    }
}
```

---

## 📊 **Pattern 4: Recent Files Management with Performance Tracking**

### **RecentFilesManager.cs - Collection Operations with Metrics**
```csharp
using LogCtxShared;
using NLogShared;

public sealed class RecentFilesManager : IRecentFilesManager
{
    private static readonly CtxLogger log = new();
    private readonly object gate = new();
    private readonly List<RecentFileInfo> items = new();
    
    public void RegisterGeneratedFile(string filePath, RecentFileType fileType, 
        IReadOnlyList<string> sourceFolders, long fileSizeBytes = 0, DateTime? generatedAtUtc = null)
    {
        using var ctx = LogCtx.Set(new Props()
            .Add("Operation", "RegisterGeneratedFile")
            .Add("FilePath", filePath)
            .Add("FileType", fileType.ToString())
            .Add("SourceFolderCount", sourceFolders.Count)
            .Add("FileSizeBytes", fileSizeBytes)
            .Add("GeneratedAt", generatedAtUtc ?? DateTime.UtcNow));
        
        LogCtx.Logger.Info("Registering new generated file");
        
        var newItem = new RecentFileInfo(filePath, 
            generatedAtUtc?.ToLocalTime() ?? DateTime.Now,
            fileType, sourceFolders.ToList(), fileSizeBytes);
        
        lock (gate)
        {
            // Remove any existing entry with same path
            int removedCount = items.RemoveAll(i => 
                i.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase));
            
            if (removedCount > 0)
            {
                ctx.Add("DuplicatesRemoved", removedCount);
                LogCtx.Logger.Debug("Removed duplicate entries");
            }
            
            items.Add(newItem);
            ctx.Add("TotalItems", items.Count);
            
            EnforcePoliciesNoLock();
            
            ctx.Add("FinalItemCount", items.Count);
            LogCtx.Logger.Info("Generated file registered successfully");
        }
        
        Save();
    }
    
    public int CleanupExpiredFiles(DateTime? nowUtc = null)
    {
        using var ctx = LogCtx.Set(new Props()
            .Add("Operation", "CleanupExpiredFiles")
            .Add("RetentionDays", config.RetentionDays)
            .Add("CurrentTime", nowUtc ?? DateTime.UtcNow));
        
        LogCtx.Logger.Info("Starting expired files cleanup");
        
        var now = nowUtc?.ToLocalTime() ?? DateTime.Now;
        var cutoff = now.AddDays(-config.RetentionDays);
        
        ctx.Add("CutoffDate", cutoff);
        
        int removedCount;
        lock (gate)
        {
            int initialCount = items.Count;
            ctx.Add("InitialItemCount", initialCount);
            
            removedCount = items.RemoveAll(f => f.GeneratedAt < cutoff);
            
            ctx.Add("ItemsRemoved", removedCount);
            ctx.Add("FinalItemCount", items.Count);
            
            if (removedCount > 0)
            {
                Save();
                LogCtx.Logger.Info("Expired files cleanup completed");
            }
            else
            {
                LogCtx.Logger.Debug("No expired files found");
            }
        }
        
        return removedCount;
    }
    
    private void EnforcePoliciesNoLock()
    {
        using var ctx = LogCtx.Set(new Props()
            .Add("Operation", "EnforcePolicies")
            .Add("MaxCount", config.MaxCount)
            .Add("CurrentCount", items.Count));
        
        if (items.Count <= config.MaxCount)
        {
            ctx.Add("PolicyEnforced", false);
            LogCtx.Logger.Debug("Item count within limits, no enforcement needed");
            return;
        }
        
        var trimmedList = items.OrderByDescending(f => f.GeneratedAt)
            .Take(config.MaxCount)
            .ToList();
        
        int removedCount = items.Count - trimmedList.Count;
        
        items.Clear();
        items.AddRange(trimmedList);
        
        ctx.Add("PolicyEnforced", true);
        ctx.Add("ItemsRemoved", removedCount);
        ctx.Add("FinalCount", items.Count);
        
        LogCtx.Logger.Info("Policy enforcement completed - excess items removed");
    }
}
```

---

## 🧪 **Pattern 5: Unit Testing with LogCtx**

### **ConvertSelectedFoldersToMDTests.cs - Test Context Tracking**
```csharp
using NUnit.Framework;
using Shouldly;
using LogCtxShared;
using NLogShared;

[TestFixture]
public class ConvertSelectedFoldersToMDTests : DocTestBase
{
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        // ✅ Initialize LogCtx once per test fixture
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
            .Add("TestRootPath", testRootPath));
            
        testRootPath = Path.Combine(Path.GetTempPath(), "ConvertSelectedFoldersToDocxTests");
        Directory.CreateDirectory(testRootPath);
        
        outputDocxPath = Path.Combine(testRootPath, "output.docx");
        
        setupCtx.Add("DirectoryCreated", true);
        setupCtx.Add("OutputPath", outputDocxPath);
        
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
        
        LogCtx.Logger.Info("Test execution started");
        
        string folder1 = Path.Combine(testRootPath, "MarkdownFolder1Name");
        string folder2 = Path.Combine(testRootPath, "MarkdownFolder2Name");
        Directory.CreateDirectory(folder1);
        Directory.CreateDirectory(folder2);
        
        string textFilePath1 = Path.Combine(folder1, "Markdown1FileName");
        string textFilePath2 = Path.Combine(folder2, "Markdown2FileName");
        
        File.WriteAllText(textFilePath1, "ContentOfMarkdownFile1");
        File.WriteAllText(textFilePath2, "ContentOfMarkdownFile2");
        
        testCtx.Add("TestDataCreated", true);
        testCtx.Add("InputFolders", 2);
        testCtx.Add("InputFiles", 2);
        
        string outputMarkdownPath = Path.Combine(testRootPath, "output.md");
        List<string> folderPaths = new List<string> { folder1, folder2 };
        
        // Act
        var stopwatch = Stopwatch.StartNew();
        
        var mdHandler = new MDHandler(null, null);
        mdHandler.ExportSelectedFolders(folderPaths, outputMarkdownPath, new VectorStoreConfig());
        
        stopwatch.Stop();
        
        testCtx.Add("ExecutionTimeMs", stopwatch.ElapsedMilliseconds);
        testCtx.Add("OutputGenerated", File.Exists(outputMarkdownPath));
        
        // Assert
        File.Exists(outputMarkdownPath).ShouldBeTrue();
        
        string markdownContent = File.ReadAllText(outputMarkdownPath);
        testCtx.Add("OutputSizeBytes", markdownContent.Length);
        
        markdownContent.ShouldContain("Folder MarkdownFolder1Name");
        markdownContent.ShouldContain("File Markdown1FileName");
        markdownContent.ShouldContain("ContentOfMarkdownFile1");
        markdownContent.ShouldContain("Folder MarkdownFolder2Name");
        markdownContent.ShouldContain("File Markdown2FileName");
        markdownContent.ShouldContain("ContentOfMarkdownFile2");
        
        testCtx.Add("AssertionsPassed", 6);
        testCtx.Add("TestResult", "Success");
        testCtx.Add("TestEndTime", DateTime.UtcNow);
        
        LogCtx.Logger.Info("Test execution completed successfully");
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
                LogCtx.Logger.Debug("Test cleanup completed");
            }
        }
        catch (Exception ex)
        {
            cleanupCtx.Add("CleanupSuccess", false);
            cleanupCtx.Add("CleanupError", ex.GetType().Name);
            LogCtx.Logger.Warn(ex, "Test cleanup encountered issues");
        }
    }
}
```

---

## 🔍 **Pattern 6: File Processing with Validation**

### **OpenXmlContentValidator.cs - Validation with Context**
```csharp
using LogCtxShared;
using NLogShared;

public class OpenXmlContentValidator : IDocumentContentValidator
{
    private static readonly CtxLogger log = new();
    private static readonly char[] NonPrintableCharacters = 
        Enumerable.Range(0, 9)    // ASCII 0-8
        .Concat(Enumerable.Range(11, 2))  // ASCII 11-12
        .Concat(Enumerable.Range(14, 18)) // ASCII 14-31
        .Select(i => (char)i)
        .ToArray();
    
    public bool IsValid(string content)
    {
        using var ctx = LogCtx.Set(new Props()
            .Add("Operation", "ValidateOpenXmlContent")
            .Add("ContentLength", content?.Length ?? 0)
            .Add("ValidationTime", DateTime.UtcNow));
        
        LogCtx.Logger.Debug("Starting OpenXML content validation");
        
        if (content == null)
        {
            ctx.Add("ValidationResult", "Valid");
            ctx.Add("ValidationReason", "NullContentTreatedAsValid");
            LogCtx.Logger.Debug("Null content treated as valid");
            return true;
        }
        
        var invalidChars = FindInvalidCharacters(content).ToList();
        bool isValid = !invalidChars.Any();
        
        ctx.Add("ValidationResult", isValid ? "Valid" : "Invalid");
        ctx.Add("InvalidCharCount", invalidChars.Count);
        
        if (!isValid)
        {
            ctx.Add("InvalidCharacters", string.Join(",", invalidChars.Select(c => $"0x{(int)c:X2}")));
            LogCtx.Logger.Warn("Content contains invalid characters for OpenXML");
        }
        else
        {
            LogCtx.Logger.Debug("Content validation passed");
        }
        
        return isValid;
    }
    
    public IEnumerable<char> FindInvalidCharacters(string content)
    {
        using var ctx = LogCtx.Set(new Props()
            .Add("Operation", "FindInvalidCharacters")
            .Add("ContentLength", content?.Length ?? 0));
        
        if (content == null)
        {
            ctx.Add("InvalidCharsFound", 0);
            return Enumerable.Empty<char>();
        }
        
        var invalidChars = NonPrintableCharacters
            .Where(content.Contains)
            .ToList();
        
        ctx.Add("InvalidCharsFound", invalidChars.Count);
        
        if (invalidChars.Any())
        {
            LogCtx.Logger.Debug("Invalid characters detected in content");
        }
        
        return invalidChars;
    }
}
```

---

## ⚡ **Pattern 7: Performance Monitoring with Batch Operations**

### **FileSizeFormatter.cs - Utility with Performance Context**
```csharp
using LogCtxShared;
using NLogShared;

public static class FileSizeFormatter
{
    private static readonly CtxLogger log = new();
    
    public static string Format(long bytes)
    {
        using var ctx = LogCtx.Set(new Props()
            .Add("Operation", "FormatFileSize")
            .Add("InputBytes", bytes)
            .Add("FormatStartTime", DateTime.UtcNow));
        
        if (bytes < 0)
        {
            ctx.Add("ValidationError", "NegativeBytes");
            LogCtx.Logger.Error("Byte size cannot be negative");
            throw new ArgumentOutOfRangeException(nameof(bytes), "Byte size cannot be negative.");
        }
        
        string[] units = { "B", "KB", "MB", "GB", "TB", "PB" };
        double size = bytes;
        int unitIndex = 0;
        
        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }
        
        string formattedSize = $"{size:0.##} {units[unitIndex]}";
        
        ctx.Add("FormattedSize", formattedSize);
        ctx.Add("UnitUsed", units[unitIndex]);
        ctx.Add("UnitIndex", unitIndex);
        ctx.Add("FormattingSuccess", true);
        
        LogCtx.Logger.Debug("File size formatting completed");
        
        return formattedSize;
    }
    
    public static string GetFileSizeFormatted(string filePath)
    {
        using var ctx = LogCtx.Set(new Props()
            .Add("Operation", "GetFormattedFileSize")
            .Add("FilePath", filePath)
            .Add("AccessTime", DateTime.UtcNow));
        
        LogCtx.Logger.Debug("Retrieving formatted file size");
        
        try
        {
            if (!File.Exists(filePath))
            {
                ctx.Add("FileExists", false);
                LogCtx.Logger.Error("File not found for size calculation");
                throw new FileNotFoundException($"File not found: {filePath}");
            }
            
            var fileInfo = new FileInfo(filePath);
            ctx.Add("FileExists", true);
            ctx.Add("FileSizeBytes", fileInfo.Length);
            ctx.Add("LastModified", fileInfo.LastWriteTime);
            
            string formatted = Format(fileInfo.Length);
            
            ctx.Add("FormattedResult", formatted);
            LogCtx.Logger.Debug("File size retrieved and formatted successfully");
            
            return formatted;
        }
        catch (UnauthorizedAccessException ex)
        {
            ctx.Add("ErrorType", "UnauthorizedAccess");
            ctx.Add("AccessDenied", true);
            LogCtx.Logger.Error(ex, "Access denied retrieving file size");
            throw;
        }
        catch (IOException ex)
        {
            ctx.Add("ErrorType", "IOException");
            ctx.Add("IOError", ex.Message);
            LogCtx.Logger.Error(ex, "IO error retrieving file size");
            throw;
        }
    }
}
```

---

## 📋 **Pattern 8: UI State Management with Persistence**

### **UiStateConfig.cs - JSON Persistence with Error Handling**
```csharp
using LogCtxShared;
using NLogShared;

public sealed class UiStateConfig
{
    private static readonly CtxLogger log = new();
    
    public static UiState Load(string? directory = null)
    {
        using var ctx = LogCtx.Set(new Props()
            .Add("Operation", "LoadUiState")
            .Add("Directory", directory)
            .Add("LoadTime", DateTime.UtcNow));
        
        LogCtx.Logger.Info("Loading UI state configuration");
        
        try
        {
            var path = ResolveUiStatePath(directory);
            ctx.Add("ResolvedPath", path);
            
            if (File.Exists(path))
            {
                var fileInfo = new FileInfo(path);
                ctx.Add("FileExists", true);
                ctx.Add("FileSizeBytes", fileInfo.Length);
                ctx.Add("LastModified", fileInfo.LastWriteTime);
                
                var json = File.ReadAllText(path);
                ctx.Add("JsonLength", json.Length);
                
                var state = JsonSerializer.Deserialize<UiState>(json);
                
                if (state != null)
                {
                    ctx.Add("LoadSuccess", true);
                    ctx.Add("ColumnCount", state.RecentFilesColumnWidths.Count);
                    ctx.Add("HasRowHeightScale", state.RecentFilesRowHeightScale.HasValue);
                    
                    LogCtx.Logger.Info("UI state loaded successfully from file");
                    return state;
                }
                else
                {
                    ctx.Add("LoadSuccess", false);
                    ctx.Add("DeserializationResult", "Null");
                    LogCtx.Logger.Warn("Deserialization returned null, using defaults");
                }
            }
            else
            {
                ctx.Add("FileExists", false);
                ctx.Add("UsingDefaults", true);
                LogCtx.Logger.Info("UI state file not found, using defaults");
            }
            
            return new UiState();
        }
        catch (JsonException ex)
        {
            ctx.Add("ErrorType", "JsonException");
            ctx.Add("JsonError", ex.Message);
            LogCtx.Logger.Warn(ex, "Invalid JSON in UI state file, using defaults");
            return new UiState();
        }
        catch (IOException ex)
        {
            ctx.Add("ErrorType", "IOException");
            ctx.Add("IOError", ex.Message);
            LogCtx.Logger.Warn(ex, "IO error loading UI state, using defaults");
            return new UiState();
        }
        catch (Exception ex)
        {
            // Defensive: never crash UI on corrupt/missing file
            ctx.Add("ErrorType", ex.GetType().Name);
            ctx.Add("UnexpectedError", ex.Message);
            LogCtx.Logger.Warn(ex, "Unexpected error loading UI state, using defaults");
            return new UiState();
        }
    }
    
    public static void Save(UiState state, string? directory = null)
    {
        using var ctx = LogCtx.Set(new Props()
            .Add("Operation", "SaveUiState")
            .Add("Directory", directory)
            .Add("SaveTime", DateTime.UtcNow));
        
        if (state is null)
        {
            ctx.Add("ErrorType", "ArgumentNull");
            LogCtx.Logger.Error("Cannot save null UI state");
            throw new ArgumentNullException(nameof(state));
        }
        
        LogCtx.Logger.Info("Saving UI state configuration");
        
        try
        {
            var path = ResolveUiStatePath(directory);
            ctx.Add("ResolvedPath", path);
            
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                ctx.Add("DirectoryCreated", true);
                LogCtx.Logger.Debug("Created directory for UI state file");
            }
            
            var json = JsonSerializer.Serialize(state, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            
            ctx.Add("JsonLength", json.Length);
            ctx.Add("ColumnCount", state.RecentFilesColumnWidths.Count);
            
            File.WriteAllText(path, json);
            
            ctx.Add("SaveSuccess", true);
            LogCtx.Logger.Info("UI state saved successfully");
        }
        catch (UnauthorizedAccessException ex)
        {
            ctx.Add("ErrorType", "UnauthorizedAccess");
            LogCtx.Logger.Warn(ex, "Access denied saving UI state");
            // Defensive: swallow to avoid UI disruption during resize/drags
        }
        catch (IOException ex)
        {
            ctx.Add("ErrorType", "IOException");
            ctx.Add("IOError", ex.Message);
            LogCtx.Logger.Warn(ex, "IO error saving UI state");
            // Defensive: swallow to avoid UI disruption
        }
        catch (Exception ex)
        {
            ctx.Add("ErrorType", ex.GetType().Name);
            LogCtx.Logger.Warn(ex, "Unexpected error saving UI state");
            // Defensive: swallow to avoid UI disruption
        }
    }
}
```

---

## 🎯 **Key VecTool LogCtx Patterns Summary**

### **✅ Initialization Pattern**
- **Failsafe startup**: `FailsafeLogger.Initialize()` never throws
- **Rich startup context**: Version, environment, process info
- **Graceful error handling**: Fatal exceptions with context

### **✅ Configuration Loading Pattern**
- **Validation with context**: Parameter validation with detailed errors
- **Fallback strategies**: Default values when config is missing/invalid
- **Parse tracking**: Log success/failure of configuration parsing

### **✅ File Operations Pattern**  
- **Path resolution**: Always log resolved paths for debugging
- **Size and metadata**: Include file size, modification times
- **Error categorization**: IOException vs UnauthorizedAccess vs JsonException

### **✅ Collection Management Pattern**
- **Policy enforcement**: Max count, retention policies with metrics
- **Duplicate handling**: Remove and count duplicates
- **Performance tracking**: Execution times, item counts

### **✅ Testing Pattern**
- **Fixture-level initialization**: `FailsafeLogger.Initialize()` in OneTimeSetUp
- **Test execution tracking**: Start/end times, execution metrics  
- **Assertion counting**: Track number of assertions passed
- **Cleanup logging**: Success/failure of test cleanup

### **✅ Validation Pattern**
- **Input validation**: Check nulls, ranges, formats
- **Result enrichment**: Include validation details in context
- **Performance measurement**: Track validation execution time

### **✅ UI State Pattern**
- **Defensive programming**: Never crash UI on file operations
- **File existence checking**: Always check before operations
- **Graceful degradation**: Use defaults when persistence fails

### **✅ Error Handling Strategy**
- **Context enrichment**: Add error type, operation details
- **Categorized logging**: Info/Warn/Error based on severity
- **Preserve exceptions**: Re-throw with additional context
- **Defensive patterns**: Swallow non-critical errors in UI operations

---

**Next Steps**: See [SEQ-Configuration-Guide.md](SEQ-Configuration-Guide.md) for querying these rich contexts in SEQ dashboards! 🚀