# LogCtx Best Practices Guide
*Consolidated coding standards and patterns for professional LogCtx development*

## 🎯 **Core Philosophy**

LogCtx follows **Clean Code principles** and **SOLID architecture** with structured logging as a first-class citizen. This guide consolidates standards from VecTool production code, GUIDE-251005-CodingConvention, and real-world best practices.

### **Fundamental Principles**
- **English Only**: All code, variables, methods, and comments must be in English
- **SOLID Compliance**: Follow Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, Dependency Inversion
- **Structured Logging**: LogCtx is the primary logging mechanism, never use NLog/Serilog directly
- **Testability**: Design for unit testing with NUnit + Shouldly
- **Defensive Programming**: Never crash on logging failures, always provide fallbacks

---

## 📋 **LogCtx-Specific Best Practices**

### **✅ DO: Context Per Significant Operation**

```csharp
// ✅ CORRECT: Each significant step gets its own context
public async Task<bool> ProcessFileAsync(string filePath)
{
    // Step 1: New context for file processing start
    using var ctx1 = LogCtx.Set(new Props()
        .Add("Operation", "ProcessFile")
        .Add("FilePath", filePath));
    LogCtx.Logger.Info("File processing initiated");
    
    try
    {
        var content = await File.ReadAllTextAsync(filePath);
        
        // Step 2: New context for validation phase
        using var ctx2 = LogCtx.Set(new Props()
            .Add("Operation", "ValidateContent")
            .Add("FilePath", filePath)
            .Add("ContentLength", content.Length));
        LogCtx.Logger.Info("Content validation started");
        
        // Your validation logic...
        
        LogCtx.Logger.Info("File processing completed successfully");
        return true;
    }
    catch (Exception ex)
    {
        // Step 3: Error context with enrichment
        using var ctxErr = LogCtx.Set(new Props()
            .Add("Operation", "ProcessFileError")
            .Add("FilePath", filePath)
            .Add("ErrorType", ex.GetType().Name));
        LogCtx.Logger.Error(ex, "File processing failed");
        return false;
    }
}
```

### **❌ DON'T: Reuse Contexts Across Operations**

```csharp
// ❌ WRONG: Context reuse loses source location accuracy
public async Task ProcessMultipleFilesAsync(List<string> files)
{
    using var ctx = LogCtx.Set(); // Created once
    
    LogCtx.Logger.Info("Starting batch processing"); // Correct location
    
    foreach (var file in files)
    {
        ProcessSingleFile(file);
        LogCtx.Logger.Info("File processed"); // WRONG: Still shows original line!
    }
}
```

### **✅ DO: Rich Property Context**

```csharp
// ✅ CORRECT: Meaningful properties for SEQ queries
using var ctx = LogCtx.Set(new Props()
    .Add("UserId", currentUser.Id)
    .Add("SessionId", sessionContext.Id)
    .Add("Operation", "DocumentExport")
    .Add("DocumentType", "PDF")
    .Add("PageCount", document.Pages.Count)
    .Add("ExportStartTime", DateTime.UtcNow));

LogCtx.Logger.Info("Document export started");
```

### **❌ DON'T: Empty or Meaningless Contexts**

```csharp
// ❌ WRONG: No contextual information
using var ctx = LogCtx.Set();
LogCtx.Logger.Info("Something happened"); // Useless for debugging

// ❌ WRONG: Vague properties
using var ctx = LogCtx.Set(new Props().Add("Data", "stuff"));
LogCtx.Logger.Info("Processing complete"); // What was processed?
```

### **✅ DO: Structured Data Types**

```csharp
// ✅ CORRECT: Proper data types for SEQ analysis
using var ctx = LogCtx.Set(new Props()
    .Add("Duration", stopwatch.ElapsedMilliseconds)    // Numeric
    .Add("Timestamp", DateTime.UtcNow)                 // DateTime
    .Add("Success", true)                              // Boolean
    .Add("ItemCount", items.Count)                     // Integer
    .Add("UserId", userId)                             // String identifier
    .Add("OperationId", Guid.NewGuid()));              // Guid
```

### **❌ DON'T: Everything as Strings**

```csharp
// ❌ WRONG: String representations lose SEQ query power
using var ctx = LogCtx.Set(new Props()
    .Add("Duration", $"{stopwatch.ElapsedMilliseconds}ms")  // Can't do math
    .Add("Timestamp", DateTime.Now.ToString())              // Can't do time queries
    .Add("Success", "true")                                 // Can't do boolean filters
    .Add("ItemCount", items.Count.ToString()));             // Can't do numeric comparisons
```

---

## 🏗️ **Class Design & Architecture**

### **✅ DO: SOLID Principles with LogCtx**

```csharp
// ✅ Single Responsibility + LogCtx integration
public sealed class DocumentProcessor : IDocumentProcessor
{
    private static readonly CtxLogger log = new();
    private readonly IFileValidator validator;
    private readonly IDocumentConverter converter;
    
    public DocumentProcessor(IFileValidator validator, IDocumentConverter converter)
    {
        this.validator = validator ?? throw new ArgumentNullException(nameof(validator));
        this.converter = converter ?? throw new ArgumentNullException(nameof(converter));
    }
    
    public async Task<ProcessResult> ProcessAsync(ProcessRequest request)
    {
        using var ctx = LogCtx.Set(new Props()
            .Add("Operation", nameof(ProcessAsync))
            .Add("RequestId", request.Id)
            .Add("DocumentType", request.DocumentType));
            
        LogCtx.Logger.Info("Document processing started");
        
        // Single responsibility: orchestrate, don't implement details
        var validationResult = await validator.ValidateAsync(request.FilePath);
        var conversionResult = await converter.ConvertAsync(request.FilePath, request.OutputFormat);
        
        return new ProcessResult(validationResult, conversionResult);
    }
}
```

### **✅ DO: Interface Segregation**

```csharp
// ✅ Focused interfaces for specific concerns
public interface IRecentFilesReader
{
    Task<IReadOnlyList<RecentFileInfo>> GetAllAsync();
    Task<IReadOnlyList<RecentFileInfo>> GetByTypeAsync(RecentFileType type);
}

public interface IRecentFilesWriter
{
    Task RegisterFileAsync(string filePath, RecentFileType type);
    Task RemoveFileAsync(string filePath);
    Task ClearExpiredAsync(DateTime cutoffUtc);
}

// Combine when both needed
public interface IRecentFilesManager : IRecentFilesReader, IRecentFilesWriter
{
    // Additional orchestration methods if needed
}
```

### **❌ DON'T: God Interfaces**

```csharp
// ❌ WRONG: Massive interface violates ISP
public interface IApplicationManager
{
    // File operations
    Task ProcessFileAsync(string path);
    Task DeleteFileAsync(string path);
    
    // User management
    Task<User> GetUserAsync(int id);
    Task SaveUserAsync(User user);
    
    // Configuration
    Task<AppConfig> LoadConfigAsync();
    Task SaveConfigAsync(AppConfig config);
    
    // Logging (LogCtx should handle this!)
    void LogInfo(string message);
    void LogError(string message);
}
```

---

## 📛 **Naming Conventions**

### **✅ DO: Descriptive and Consistent Names**

```csharp
// ✅ Classes and Interfaces
public class DocumentExportHandler : IDocumentExporter
public interface IRecentFilesManager
public sealed class VectorStoreConfiguration

// ✅ Methods and Properties
public async Task<ExportResult> ExportToMarkdownAsync(List<string> folderPaths)
public bool IsFileExcluded(string fileName, VectorStoreConfig config)
public string OutputDirectory { get; set; }

// ✅ Private fields (underscore prefix)
private readonly IUserInterface _ui;
private readonly string _workingDirectory;
private static readonly CtxLogger _log = new();

// ✅ Local variables and parameters
public void ProcessFolder(string folderPath, VectorStoreConfig vectorStoreConfig)
{
    var folderName = new DirectoryInfo(folderPath).Name;
    for (int i = 0; i < files.Length; i++) // i is acceptable for loops
    {
        // Process file...
    }
}

// ✅ Constants
public const string DefaultOutputPath = "Generated";
private const string CONFIG_KEY_MAX_COUNT = "recentFilesMaxCount";
```

### **❌ DON'T: Inconsistent or Unclear Names**

```csharp
// ❌ WRONG: Inconsistent field naming
private IUserInterface ui;           // Missing underscore
private readonly string workingDir;  // Abbreviated, no underscore

// ❌ WRONG: Unclear method names
public void DoStuff(string path)     // What stuff?
public bool Check(string file)       // Check what?
public void Handle(object data)      // Handle how?

// ❌ WRONG: Inconsistent casing
public void convertSelectedFolders() // Should be PascalCase
private bool isValid;                 // Field should have underscore
```

---

## 🔧 **Error Handling Patterns**

### **✅ DO: Defensive Error Handling with LogCtx**

```csharp
public async Task<LoadResult> LoadConfigurationAsync(string configPath)
{
    using var ctx = LogCtx.Set(new Props()
        .Add("Operation", "LoadConfiguration")
        .Add("ConfigPath", configPath));
        
    LogCtx.Logger.Info("Configuration loading started");
    
    try
    {
        if (!File.Exists(configPath))
        {
            ctx.Add("ConfigExists", false);
            ctx.Add("UsingDefaults", true);
            LogCtx.Logger.Info("Configuration file not found, using defaults");
            return LoadResult.FromDefaults();
        }
        
        var json = await File.ReadAllTextAsync(configPath);
        ctx.Add("ConfigSizeBytes", json.Length);
        
        var config = JsonSerializer.Deserialize<AppConfig>(json);
        
        if (config == null)
        {
            ctx.Add("DeserializationResult", "Null");
            LogCtx.Logger.Warn("Deserialization returned null, using defaults");
            return LoadResult.FromDefaults();
        }
        
        ctx.Add("LoadSuccess", true);
        LogCtx.Logger.Info("Configuration loaded successfully");
        return LoadResult.FromConfig(config);
    }
    catch (UnauthorizedAccessException ex)
    {
        ctx.Add("ErrorType", "UnauthorizedAccess");
        LogCtx.Logger.Error(ex, "Access denied loading configuration");
        return LoadResult.FromError("Access denied", ex);
    }
    catch (JsonException ex)
    {
        ctx.Add("ErrorType", "JsonException");
        ctx.Add("JsonError", ex.Message);
        LogCtx.Logger.Error(ex, "Invalid JSON in configuration file");
        return LoadResult.FromError("Invalid configuration format", ex);
    }
    catch (IOException ex)
    {
        ctx.Add("ErrorType", "IOException");
        LogCtx.Logger.Error(ex, "IO error loading configuration");
        return LoadResult.FromError("File access error", ex);
    }
    catch (Exception ex)
    {
        // Defensive: never crash on configuration loading
        ctx.Add("ErrorType", ex.GetType().Name);
        LogCtx.Logger.Error(ex, "Unexpected error loading configuration");
        return LoadResult.FromDefaults(); // Graceful degradation
    }
}
```

### **✅ DO: Specific Exception Types**

```csharp
// ✅ Custom exceptions for domain-specific errors
public class TestExecutionException : Exception
{
    public int ExitCode { get; }
    public string StandardError { get; }
    
    public TestExecutionException(int exitCode, string standardError, string message) 
        : base(message)
    {
        ExitCode = exitCode;
        StandardError = standardError;
    }
}

// Usage
public async Task RunTestsAsync()
{
    using var ctx = LogCtx.Set(new Props().Add("Operation", "RunTests"));
    
    try
    {
        var result = await _processRunner.RunAsync("dotnet", "test");
        
        if (result.ExitCode != 0)
        {
            ctx.Add("ExitCode", result.ExitCode);
            ctx.Add("StandardError", result.StandardError);
            
            throw new TestExecutionException(
                result.ExitCode, 
                result.StandardError,
                $"Tests failed with exit code {result.ExitCode}");
        }
    }
    catch (TestExecutionException)
    {
        LogCtx.Logger.Error("Test execution failed");
        throw; // Re-throw specific exceptions
    }
    catch (Exception ex)
    {
        LogCtx.Logger.Error(ex, "Unexpected error during test execution");
        throw;
    }
}
```

---

## 🧪 **Testing Best Practices**

### **✅ DO: LogCtx-Integrated Testing**

```csharp
[TestFixture]
public class DocumentProcessorTests
{
    private DocumentProcessor _processor;
    private Mock<IFileValidator> _mockValidator;
    private Mock<IDocumentConverter> _mockConverter;
    
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        // ✅ Initialize LogCtx once per test fixture
        FailsafeLogger.Initialize();
    }
    
    [SetUp]
    public void Setup()
    {
        _mockValidator = new Mock<IFileValidator>();
        _mockConverter = new Mock<IDocumentConverter>();
        _processor = new DocumentProcessor(_mockValidator.Object, _mockConverter.Object);
    }
    
    [Test]
    public async Task ProcessAsync_ValidDocument_ShouldSucceed()
    {
        // Arrange
        using var testCtx = LogCtx.Set(new Props()
            .Add("TestMethod", nameof(ProcessAsync_ValidDocument_ShouldSucceed))
            .Add("TestCategory", "DocumentProcessing")
            .Add("TestStartTime", DateTime.UtcNow));
            
        LogCtx.Logger.Info("Test execution started");
        
        var request = new ProcessRequest 
        { 
            Id = Guid.NewGuid(),
            FilePath = "test-document.pdf",
            DocumentType = "PDF"
        };
        
        _mockValidator.Setup(v => v.ValidateAsync(request.FilePath))
                     .ReturnsAsync(ValidationResult.Success);
                     
        _mockConverter.Setup(c => c.ConvertAsync(request.FilePath, It.IsAny<string>()))
                     .ReturnsAsync(ConversionResult.Success);
        
        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await _processor.ProcessAsync(request);
        stopwatch.Stop();
        
        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        
        _mockValidator.Verify(v => v.ValidateAsync(request.FilePath), Times.Once);
        _mockConverter.Verify(c => c.ConvertAsync(request.FilePath, It.IsAny<string>()), Times.Once);
        
        testCtx.Add("ExecutionTimeMs", stopwatch.ElapsedMilliseconds);
        testCtx.Add("TestResult", "Success");
        LogCtx.Logger.Info("Test execution completed successfully");
    }
    
    [Test]
    public void ProcessAsync_NullValidator_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var testCtx = LogCtx.Set(new Props()
            .Add("TestMethod", nameof(ProcessAsync_NullValidator_ShouldThrowArgumentNullException))
            .Add("TestCategory", "Validation"));
            
        LogCtx.Logger.Info("Exception test started");
        
        // Act & Assert
        var exception = Should.Throw<ArgumentNullException>(() =>
            new DocumentProcessor(null, _mockConverter.Object));
        
        exception.ParamName.ShouldBe("validator");
        
        testCtx.Add("ExpectedException", nameof(ArgumentNullException));
        LogCtx.Logger.Info("Exception test completed successfully");
    }
}
```

### **✅ DO: Test Method Naming**

```csharp
// ✅ Pattern: MethodName_Scenario_ExpectedBehavior
[Test]
public void ConvertSelectedFoldersToMarkdown_EmptyFolderList_ShouldReturnEmptyResult()

[Test]
public void LoadConfiguration_MissingConfigFile_ShouldReturnDefaults()

[Test]
public void ProcessFile_InvalidFilePath_ShouldThrowArgumentException()

[Test]
public async Task RunTestsAsync_NonZeroExitCode_ShouldThrowTestExecutionException()
```

---

## ⚡ **Performance Best Practices**

### **✅ DO: Async/Await Patterns**

```csharp
// ✅ Proper async implementation
public async Task<ProcessResult> ProcessMultipleFilesAsync(List<string> filePaths)
{
    using var ctx = LogCtx.Set(new Props()
        .Add("Operation", "ProcessMultipleFiles")
        .Add("FileCount", filePaths.Count));
        
    LogCtx.Logger.Info("Batch processing started");
    
    var results = new List<ProcessResult>();
    
    // Process files concurrently with controlled parallelism
    var semaphore = new SemaphoreSlim(Environment.ProcessorCount);
    var tasks = filePaths.Select(async filePath =>
    {
        await semaphore.WaitAsync();
        try
        {
            return await ProcessSingleFileAsync(filePath).ConfigureAwait(false);
        }
        finally
        {
            semaphore.Release();
        }
    });
    
    var allResults = await Task.WhenAll(tasks).ConfigureAwait(false);
    
    ctx.Add("SuccessfulCount", allResults.Count(r => r.IsSuccess));
    ctx.Add("FailedCount", allResults.Count(r => !r.IsSuccess));
    LogCtx.Logger.Info("Batch processing completed");
    
    return ProcessResult.FromBatch(allResults);
}
```

### **✅ DO: Memory Management**

```csharp
// ✅ Proper disposal patterns
public class FileProcessor : IDisposable
{
    private readonly SemaphoreSlim _semaphore;
    private bool _disposed;
    
    public FileProcessor()
    {
        _semaphore = new SemaphoreSlim(Environment.ProcessorCount);
    }
    
    public async Task<ProcessResult> ProcessAsync(string filePath)
    {
        ThrowIfDisposed();
        
        using var ctx = LogCtx.Set(new Props()
            .Add("Operation", "ProcessFile")
            .Add("FilePath", filePath));
            
        await _semaphore.WaitAsync();
        try
        {
            // Use using statements for disposable resources
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var reader = new StreamReader(fileStream);
            
            var content = await reader.ReadToEndAsync();
            
            // Process content...
            
            LogCtx.Logger.Info("File processing completed");
            return ProcessResult.Success;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(FileProcessor));
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            _semaphore?.Dispose();
            _disposed = true;
        }
    }
}
```

---

## 📝 **Documentation Standards**

### **✅ DO: XML Documentation**

```csharp
/// <summary>
/// Processes selected folders and exports them to a single Markdown file with AI-optimized content structure.
/// </summary>
/// <param name="folderPaths">List of absolute folder paths to process. Must not be null or empty.</param>
/// <param name="outputPath">Absolute path where the Markdown file will be created. Directory will be created if it doesn't exist.</param>
/// <param name="vectorStoreConfig">Configuration object specifying file exclusions and processing rules.</param>
/// <returns>A task that represents the asynchronous export operation. The task result contains the export status.</returns>
/// <exception cref="ArgumentNullException">Thrown when <paramref name="folderPaths"/> or <paramref name="vectorStoreConfig"/> is null.</exception>
/// <exception cref="ArgumentException">Thrown when <paramref name="folderPaths"/> is empty or <paramref name="outputPath"/> is invalid.</exception>
/// <exception cref="IOException">Thrown when file system operations fail.</exception>
public async Task<ExportResult> ExportSelectedFoldersAsync(
    IReadOnlyList<string> folderPaths, 
    string outputPath, 
    VectorStoreConfig vectorStoreConfig)
{
    // Implementation...
}
```

### **✅ DO: Meaningful Comments**

```csharp
public async Task<ProcessResult> ProcessBatchAsync(List<string> items)
{
    using var ctx = LogCtx.Set(new Props().Add("BatchSize", items.Count));
    
    // Use semaphore to prevent resource exhaustion on large batches
    var semaphore = new SemaphoreSlim(Environment.ProcessorCount);
    
    // Process items with controlled parallelism
    var tasks = items.Select(async item =>
    {
        await semaphore.WaitAsync();
        try
        {
            // Each item gets its own context for accurate source location
            using var itemCtx = LogCtx.Set(new Props().Add("Item", item));
            return await ProcessSingleItemAsync(item);
        }
        finally
        {
            semaphore.Release();
        }
    });
    
    // Wait for all items to complete, preserving individual results
    var results = await Task.WhenAll(tasks);
    
    return ProcessResult.FromBatch(results);
}
```

---

## 🔒 **Security Best Practices**

### **✅ DO: Secure Logging**

```csharp
public async Task<AuthResult> AuthenticateUserAsync(string username, string password)
{
    // ✅ Never log sensitive data
    using var ctx = LogCtx.Set(new Props()
        .Add("Operation", "AuthenticateUser")
        .Add("Username", username)    // OK: Username is not sensitive
        .Add("AuthStartTime", DateTime.UtcNow));
        // ❌ NEVER: .Add("Password", password) - Never log passwords!
        
    LogCtx.Logger.Info("User authentication started");
    
    try
    {
        var user = await _userRepository.GetByUsernameAsync(username);
        
        if (user == null)
        {
            ctx.Add("AuthResult", "UserNotFound");
            LogCtx.Logger.Warn("Authentication failed: user not found");
            return AuthResult.Failed("Invalid credentials");
        }
        
        var passwordValid = _passwordHasher.Verify(password, user.PasswordHash);
        
        if (!passwordValid)
        {
            ctx.Add("AuthResult", "InvalidPassword");
            LogCtx.Logger.Warn("Authentication failed: invalid password");
            return AuthResult.Failed("Invalid credentials");
        }
        
        ctx.Add("AuthResult", "Success");
        ctx.Add("UserId", user.Id);  // OK: User ID is not sensitive
        LogCtx.Logger.Info("User authenticated successfully");
        
        return AuthResult.Success(user);
    }
    catch (Exception ex)
    {
        ctx.Add("AuthResult", "Error");
        LogCtx.Logger.Error(ex, "Authentication error occurred");
        return AuthResult.Failed("Authentication error");
    }
}
```

### **✅ DO: Input Validation**

```csharp
public async Task<ProcessResult> ProcessFileAsync(string filePath)
{
    // ✅ Validate inputs early with clear error messages
    if (string.IsNullOrWhiteSpace(filePath))
        throw new ArgumentException("File path is required and cannot be empty.", nameof(filePath));
        
    if (!Path.IsPathRooted(filePath))
        throw new ArgumentException("File path must be absolute.", nameof(filePath));
        
    if (!File.Exists(filePath))
        throw new FileNotFoundException($"File not found: {filePath}");
        
    // ✅ Validate file size to prevent resource exhaustion
    var fileInfo = new FileInfo(filePath);
    const long MaxFileSizeBytes = 100 * 1024 * 1024; // 100 MB
    
    if (fileInfo.Length > MaxFileSizeBytes)
        throw new ArgumentException($"File too large: {fileInfo.Length} bytes (max: {MaxFileSizeBytes})");
    
    using var ctx = LogCtx.Set(new Props()
        .Add("Operation", "ProcessFile")
        .Add("FilePath", filePath)
        .Add("FileSizeBytes", fileInfo.Length));
        
    LogCtx.Logger.Info("File processing started");
    
    // Process file safely...
}
```

---

## 🎯 **Code Review Checklist**

### **Essential LogCtx Checks**
- [ ] **One context per operation**: Each significant step has its own `LogCtx.Set()`
- [ ] **Rich property context**: Meaningful properties added before logging
- [ ] **Proper data types**: Numbers as numbers, booleans as booleans, dates as DateTime
- [ ] **No context reuse**: Contexts are not reused across different operations
- [ ] **Error enrichment**: Exception contexts include operation details and error types

### **Architecture Checks**
- [ ] **SOLID principles**: Single responsibility, dependency injection, interface segregation
- [ ] **Naming conventions**: PascalCase public, camelCase private with underscore prefix
- [ ] **English throughout**: All code, variables, methods, and comments in English
- [ ] **Async/await properly**: ConfigureAwait(false) in libraries, proper exception handling

### **Error Handling**
- [ ] **Specific exceptions**: Custom exception types for domain errors
- [ ] **Defensive programming**: Graceful degradation when possible
- [ ] **LogCtx error context**: Error details logged with structured properties
- [ ] **Never crash on logging**: Logging failures don't break application flow

### **Testing Standards**
- [ ] **FailsafeLogger.Initialize()**: Called once in OneTimeSetUp
- [ ] **Test method naming**: MethodName_Scenario_ExpectedBehavior pattern
- [ ] **Shouldly assertions**: Used for all assertions with meaningful messages
- [ ] **LogCtx test context**: Test execution tracked with structured properties

### **Security & Performance**
- [ ] **No sensitive data**: Passwords, tokens, PII never logged
- [ ] **Input validation**: Early validation with clear error messages
- [ ] **Resource disposal**: Using statements for IDisposable resources
- [ ] **Controlled parallelism**: SemaphoreSlim for batch operations

---

## 🎯 **Quick Reference**

### **Initialization Pattern**
```csharp
// Application startup
FailsafeLogger.Initialize("NLog.config");

// Test fixtures
[OneTimeSetUp]
public void OneTimeSetup() => FailsafeLogger.Initialize();
```

### **Context Pattern**
```csharp
using var ctx = LogCtx.Set(new Props()
    .Add("Operation", "OperationName")
    .Add("Key1", value1)
    .Add("Key2", value2));
LogCtx.Logger.Info("Message describing what's happening");
```

### **Error Pattern**
```csharp
try
{
    // Operation...
}
catch (SpecificException ex)
{
    ctx.Add("ErrorType", nameof(SpecificException));
    ctx.Add("ErrorDetail", ex.Message);
    LogCtx.Logger.Error(ex, "Specific error occurred");
    throw;
}
```

### **Test Pattern**
```csharp
[Test]
public void Method_Scenario_ExpectedBehavior()
{
    using var testCtx = LogCtx.Set(new Props()
        .Add("TestMethod", nameof(Method_Scenario_ExpectedBehavior)));
    LogCtx.Logger.Info("Test execution started");
    
    // Test logic...
    
    result.ShouldBe(expectedValue);
    LogCtx.Logger.Info("Test execution completed");
}
```

---

**Next Steps**: See [Testing-Patterns.md](Testing-Patterns.md) for comprehensive NUnit + Shouldly + LogCtx testing strategies! 🚀