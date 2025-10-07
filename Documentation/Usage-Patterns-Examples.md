# LogCtx Usage Patterns & Examples

**Real-world usage patterns and advanced examples for LogCtx structured logging - corrected for version 0.3.1**

This document provides comprehensive examples of LogCtx usage patterns, from basic scenarios to advanced enterprise patterns.

---

## 🚨 **CRITICAL: Always Use Correct Initialization**

### **❌ WRONG - Will Cause Application Crashes:**
```csharp
LogCtx.InitLogCtx(); // ❌ THIS METHOD DOESN'T EXIST!
```

### **✅ CORRECT - Only Valid Initialization:**
```csharp
using NLogShared;   // Required for FailsafeLogger
using LogCtxShared; // Required for LogCtx classes

// ⚠️ MANDATORY - This is the ONLY way to initialize LogCtx!
FailsafeLogger.Initialize("NLog.config");
```

---

## 🏗️ **Pattern 1: Application Lifecycle Management**

### **Complete Application Setup with Graceful Error Handling**

```csharp
using NLogShared;   // Required for FailsafeLogger
using LogCtxShared; // Required for LogCtx classes
using NLog;

namespace VecTool.Application
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            try
            {
                // ⚠️ MANDATORY - Initialize logging before any other operations
                FailsafeLogger.Initialize("NLog.config");
                
                // Application startup sequence with detailed context
                using var startupCtx = LogCtx.Set();
                startupCtx.AddProperty("ApplicationName", "VecTool");
                startupCtx.AddProperty("Version", "4.0.p3");
                startupCtx.AddProperty("Environment", GetEnvironmentName());
                startupCtx.AddProperty("StartupTime", DateTime.UtcNow);
                LogCtx.Logger.Information("Application initialization started", startupCtx);

                // Initialize application components
                InitializeApplication(startupCtx);
                
                // Run main application loop
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
                
            }
            catch (Exception ex)
            {
                HandleStartupFailure(ex);
                throw;
            }
            finally
            {
                // Graceful shutdown logging
                using var shutdownCtx = LogCtx.Set();
                shutdownCtx.AddProperty("ApplicationName", "VecTool");
                shutdownCtx.AddProperty("ShutdownTime", DateTime.UtcNow);
                LogCtx.Logger.Information("Application shutdown completed", shutdownCtx);
            }
        }

        private static void InitializeApplication(LogCtx startupCtx)
        {
            // Component initialization with inherited context
            startupCtx.AddProperty("InitializationStep", "Components");
            LogCtx.Logger.Information("Initializing application components", startupCtx);

            // Initialize individual components...
            InitializeGitRunner(startupCtx);
            InitializeRecentFilesManager(startupCtx);
            InitializeTestRunner(startupCtx);
        }

        private static void HandleStartupFailure(Exception ex)
        {
            try
            {
                using var errorCtx = LogCtx.Set();
                errorCtx.AddProperty("ApplicationName", "VecTool");
                errorCtx.AddProperty("FailureStage", "Startup");
                errorCtx.AddProperty("ErrorType", ex.GetType().Name);
                errorCtx.AddProperty("ErrorMessage", ex.Message);
                LogCtx.Logger.Fatal("Application startup failed", ex, errorCtx);
            }
            catch
            {
                // Fallback logging if LogCtx fails
                Console.WriteLine($"CRITICAL: Startup failure - {ex}");
            }
        }

        private static string GetEnvironmentName()
        {
            #if DEBUG
                return "Development";
            #else
                return "Production";
            #endif
        }
    }
}
```

---

## 🔄 **Pattern 2: Service Layer with Transaction Context**

### **File Processing Service with Comprehensive Logging**

```csharp
using NLogShared;   // Required for FailsafeLogger
using LogCtxShared; // Required for LogCtx classes

public class FileProcessingService
{
    private readonly IFileValidator fileValidator;
    private readonly IContentProcessor contentProcessor;

    public FileProcessingService(IFileValidator fileValidator, IContentProcessor contentProcessor)
    {
        this.fileValidator = fileValidator ?? throw new ArgumentNullException(nameof(fileValidator));
        this.contentProcessor = contentProcessor ?? throw new ArgumentNullException(nameof(contentProcessor));
    }

    public async Task<ProcessingResult> ProcessFileAsync(string filePath, ProcessingOptions options)
    {
        // Create operation context with automatic source location capture
        using var operationCtx = LogCtx.Set(); // ✅ Captures file/line automatically
        operationCtx.AddProperty("ServiceName", nameof(FileProcessingService));
        operationCtx.AddProperty("Operation", nameof(ProcessFileAsync));
        operationCtx.AddProperty("FilePath", filePath);
        operationCtx.AddProperty("Options", options.ToString());
        operationCtx.AddProperty("CorrelationId", Guid.NewGuid().ToString());
        
        var stopwatch = Stopwatch.StartNew();
        LogCtx.Logger.Information("File processing operation started", operationCtx);

        try
        {
            // Step 1: File validation with metrics
            using var validationCtx = LogCtx.Set();
            validationCtx.AddProperty("ServiceName", nameof(FileProcessingService));
            validationCtx.AddProperty("Operation", "ValidateFile");
            validationCtx.AddProperty("FilePath", filePath);
            LogCtx.Logger.Information("File validation started", validationCtx);

            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
            {
                validationCtx.AddProperty("ValidationResult", "FileNotFound");
                LogCtx.Logger.Warning("File not found during validation", validationCtx);
                return ProcessingResult.Failed("File not found");
            }

            validationCtx.AddProperty("FileSize", fileInfo.Length);
            validationCtx.AddProperty("FileExtension", fileInfo.Extension);
            validationCtx.AddProperty("LastModified", fileInfo.LastWriteTimeUtc);
            validationCtx.AddProperty("ValidationResult", "Success");
            LogCtx.Logger.Information("File validation completed", validationCtx);

            // Step 2: Content processing with progress tracking
            var content = await File.ReadAllTextAsync(filePath);
            using var processingCtx = LogCtx.Set();
            processingCtx.AddProperty("ServiceName", nameof(FileProcessingService));
            processingCtx.AddProperty("Operation", "ProcessContent");
            processingCtx.AddProperty("FilePath", filePath);
            processingCtx.AddProperty("ContentLength", content.Length);
            processingCtx.AddProperty("ProcessingOptions", options.ToString());
            LogCtx.Logger.Information("Content processing started", processingCtx);

            var result = await contentProcessor.ProcessAsync(content, options);
            
            processingCtx.AddProperty("ProcessedLines", result.LinesProcessed);
            processingCtx.AddProperty("ProcessedItems", result.ItemsProcessed);
            processingCtx.AddProperty("ProcessingResult", "Success");
            LogCtx.Logger.Information("Content processing completed", processingCtx);

            // Step 3: Success metrics and final result
            stopwatch.Stop();
            operationCtx.AddProperty("TotalDurationMs", stopwatch.ElapsedMilliseconds);
            operationCtx.AddProperty("ProcessingResult", "Success");
            operationCtx.AddProperty("LinesProcessed", result.LinesProcessed);
            operationCtx.AddProperty("ItemsProcessed", result.ItemsProcessed);
            LogCtx.Logger.Information("File processing operation completed successfully", operationCtx);

            return ProcessingResult.Success(result);
        }
        catch (Exception ex)
        {
            // Comprehensive error context with performance data
            stopwatch.Stop();
            using var errorCtx = LogCtx.Set();
            errorCtx.AddProperty("ServiceName", nameof(FileProcessingService));
            errorCtx.AddProperty("Operation", nameof(ProcessFileAsync));
            errorCtx.AddProperty("FilePath", filePath);
            errorCtx.AddProperty("ErrorType", ex.GetType().Name);
            errorCtx.AddProperty("ErrorMessage", ex.Message);
            errorCtx.AddProperty("DurationBeforeFailureMs", stopwatch.ElapsedMilliseconds);
            errorCtx.AddProperty("ProcessingResult", "Failed");
            LogCtx.Logger.Error("File processing operation failed", ex, errorCtx);
            
            return ProcessingResult.Failed(ex.Message);
        }
    }
}
```

---

## 🧪 **Pattern 3: Comprehensive Test Suite Integration**

### **Integration Test Base with Setup/Teardown**

```csharp
using NUnit.Framework;
using Shouldly;
using NLogShared;   // Required for FailsafeLogger
using LogCtxShared; // Required for LogCtx classes

[TestFixture]
public abstract class IntegrationTestBase
{
    protected string TestDataDirectory { get; private set; } = string.Empty;
    protected TestContext TestContext { get; private set; } = null!;

    [OneTimeSetUp]
    public void GlobalSetup()
    {
        // ⚠️ MANDATORY INITIALIZATION - Initialize once per test fixture
        FailsafeLogger.Initialize("NLog.config");
        
        using var setupCtx = LogCtx.Set();
        setupCtx.AddProperty("TestSuite", GetType().Name);
        setupCtx.AddProperty("TestType", "Integration");
        setupCtx.AddProperty("TestFramework", "NUnit");
        setupCtx.AddProperty("SetupTime", DateTime.UtcNow);
        LogCtx.Logger.Information("Integration test suite initialization started", setupCtx);

        // Setup test environment
        TestDataDirectory = CreateTestDataDirectory();
        TestContext = CreateTestContext();
        
        setupCtx.AddProperty("TestDataDirectory", TestDataDirectory);
        setupCtx.AddProperty("TestContextId", TestContext.Id);
        LogCtx.Logger.Information("Integration test suite initialization completed", setupCtx);
    }

    [OneTimeTearDown]
    public void GlobalTearDown()
    {
        using var teardownCtx = LogCtx.Set();
        teardownCtx.AddProperty("TestSuite", GetType().Name);
        teardownCtx.AddProperty("TestType", "Integration");
        teardownCtx.AddProperty("TeardownTime", DateTime.UtcNow);
        LogCtx.Logger.Information("Integration test suite teardown started", teardownCtx);

        // Cleanup test resources
        CleanupTestData();
        TestContext?.Dispose();
        
        LogCtx.Logger.Information("Integration test suite teardown completed", teardownCtx);
    }

    [SetUp]
    public void TestSetup()
    {
        using var testSetupCtx = LogCtx.Set();
        testSetupCtx.AddProperty("TestSuite", GetType().Name);
        testSetupCtx.AddProperty("TestMethod", TestContext.CurrentContext.Test.Name);
        testSetupCtx.AddProperty("TestCategory", GetTestCategory());
        LogCtx.Logger.Information("Individual test setup started", testSetupCtx);
    }

    [TearDown]
    public void TestTearDown()
    {
        using var testTeardownCtx = LogCtx.Set();
        testTeardownCtx.AddProperty("TestSuite", GetType().Name);
        testTeardownCtx.AddProperty("TestMethod", TestContext.CurrentContext.Test.Name);
        testTeardownCtx.AddProperty("TestResult", TestContext.CurrentContext.Result.Outcome.Status.ToString());
        testTeardownCtx.AddProperty("TestDuration", TestContext.CurrentContext.Result.Duration);
        
        if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed)
        {
            testTeardownCtx.AddProperty("FailureMessage", TestContext.CurrentContext.Result.Message);
            LogCtx.Logger.Warning("Test completed with failure", testTeardownCtx);
        }
        else
        {
            LogCtx.Logger.Information("Test completed successfully", testTeardownCtx);
        }
    }

    protected virtual string GetTestCategory() => "General";
    protected abstract string CreateTestDataDirectory();
    protected abstract TestContext CreateTestContext();
    protected abstract void CleanupTestData();
}

[TestFixture]
public class FileProcessingIntegrationTests : IntegrationTestBase
{
    private FileProcessingService fileProcessingService = null!;

    [SetUp]
    public void Setup()
    {
        fileProcessingService = new FileProcessingService(
            new FileValidator(), 
            new ContentProcessor()
        );
    }

    [Test]
    [Category("FileProcessing")]
    public async Task ProcessFileAsync_ValidMarkdownFile_ShouldSucceed()
    {
        // Arrange
        using var testCtx = LogCtx.Set();
        testCtx.AddProperty("TestClass", nameof(FileProcessingIntegrationTests));
        testCtx.AddProperty("TestMethod", nameof(ProcessFileAsync_ValidMarkdownFile_ShouldSucceed));
        testCtx.AddProperty("TestCategory", "FileProcessing");
        testCtx.AddProperty("FileType", "Markdown");
        LogCtx.Logger.Information("Test execution started", testCtx);

        var testFile = Path.Combine(TestDataDirectory, "sample.md");
        await File.WriteAllTextAsync(testFile, "# Test Document\n\nThis is a test markdown file.");
        
        testCtx.AddProperty("TestFilePath", testFile);
        testCtx.AddProperty("TestFileSize", new FileInfo(testFile).Length);
        LogCtx.Logger.Information("Test file created", testCtx);

        try
        {
            // Act
            var options = new ProcessingOptions { Format = ProcessingFormat.Markdown };
            var result = await fileProcessingService.ProcessFileAsync(testFile, options);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.LinesProcessed.Should().BeGreaterThan(0);

            testCtx.AddProperty("ProcessingResult", "Success");
            testCtx.AddProperty("LinesProcessed", result.Data.LinesProcessed);
            testCtx.AddProperty("ItemsProcessed", result.Data.ItemsProcessed);
            LogCtx.Logger.Information("Test assertions passed", testCtx);
        }
        catch (Exception ex)
        {
            using var errorCtx = LogCtx.Set();
            errorCtx.AddProperty("TestClass", nameof(FileProcessingIntegrationTests));
            errorCtx.AddProperty("TestMethod", nameof(ProcessFileAsync_ValidMarkdownFile_ShouldSucceed));
            errorCtx.AddProperty("ErrorType", ex.GetType().Name);
            errorCtx.AddProperty("TestResult", "Failed");
            LogCtx.Logger.Error("Test execution failed", ex, errorCtx);
            throw;
        }

        LogCtx.Logger.Information("Test execution completed successfully", testCtx);
    }

    protected override string GetTestCategory() => "FileProcessing";

    protected override string CreateTestDataDirectory()
    {
        var dir = Path.Combine(Path.GetTempPath(), "LogCtxTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        return dir;
    }

    protected override TestContext CreateTestContext()
    {
        return new TestContext(Guid.NewGuid().ToString());
    }

    protected override void CleanupTestData()
    {
        if (Directory.Exists(TestDataDirectory))
        {
            try
            {
                Directory.Delete(TestDataDirectory, true);
            }
            catch (Exception ex)
            {
                using var cleanupCtx = LogCtx.Set();
                cleanupCtx.AddProperty("TestDataDirectory", TestDataDirectory);
                cleanupCtx.AddProperty("ErrorType", ex.GetType().Name);
                LogCtx.Logger.Warning("Failed to cleanup test data directory", ex, cleanupCtx);
            }
        }
    }
}
```

---

## 🔁 **Pattern 4: Batch Processing with Progress Tracking**

```csharp
using NLogShared;   // Required for FailsafeLogger
using LogCtxShared; // Required for LogCtx classes

public class BatchProcessor<T>
{
    private readonly int batchSize;
    private readonly IItemProcessor<T> itemProcessor;

    public BatchProcessor(IItemProcessor<T> itemProcessor, int batchSize = 100)
    {
        this.itemProcessor = itemProcessor ?? throw new ArgumentNullException(nameof(itemProcessor));
        this.batchSize = batchSize > 0 ? batchSize : throw new ArgumentException("Batch size must be positive", nameof(batchSize));
    }

    public async Task<BatchResult> ProcessAsync(IEnumerable<T> items, CancellationToken cancellationToken = default)
    {
        var itemsList = items.ToList();
        
        // Create batch operation context
        using var batchCtx = LogCtx.Set(); // ✅ Captures file/line automatically
        batchCtx.AddProperty("ServiceName", nameof(BatchProcessor<T>));
        batchCtx.AddProperty("Operation", nameof(ProcessAsync));
        batchCtx.AddProperty("ItemType", typeof(T).Name);
        batchCtx.AddProperty("TotalItems", itemsList.Count);
        batchCtx.AddProperty("BatchSize", batchSize);
        batchCtx.AddProperty("OperationId", Guid.NewGuid().ToString());
        
        var totalBatches = (int)Math.Ceiling((double)itemsList.Count / batchSize);
        batchCtx.AddProperty("TotalBatches", totalBatches);
        
        var stopwatch = Stopwatch.StartNew();
        LogCtx.Logger.Information("Batch processing operation started", batchCtx);

        var processedCount = 0;
        var failedCount = 0;
        var batchNumber = 1;

        try
        {
            foreach (var batch in itemsList.Chunk(batchSize))
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Create batch-specific context
                using var currentBatchCtx = LogCtx.Set();
                currentBatchCtx.AddProperty("ServiceName", nameof(BatchProcessor<T>));
                currentBatchCtx.AddProperty("Operation", "ProcessBatch");
                currentBatchCtx.AddProperty("ItemType", typeof(T).Name);
                currentBatchCtx.AddProperty("BatchNumber", batchNumber);
                currentBatchCtx.AddProperty("BatchSize", batch.Length);
                currentBatchCtx.AddProperty("TotalBatches", totalBatches);
                currentBatchCtx.AddProperty("ProcessedSoFar", processedCount);
                
                var batchStopwatch = Stopwatch.StartNew();
                LogCtx.Logger.Information("Processing batch started", currentBatchCtx);

                try
                {
                    // Process items in parallel within the batch
                    var tasks = batch.Select(async item =>
                    {
                        try
                        {
                            await itemProcessor.ProcessAsync(item, cancellationToken);
                            return new { Success = true, Item = item, Error = (Exception?)null };
                        }
                        catch (Exception ex)
                        {
                            return new { Success = false, Item = item, Error = ex };
                        }
                    });

                    var results = await Task.WhenAll(tasks);
                    
                    var batchSuccessCount = results.Count(r => r.Success);
                    var batchFailCount = results.Count(r => !r.Success);
                    
                    processedCount += batchSuccessCount;
                    failedCount += batchFailCount;
                    
                    batchStopwatch.Stop();
                    currentBatchCtx.AddProperty("BatchSuccessCount", batchSuccessCount);
                    currentBatchCtx.AddProperty("BatchFailCount", batchFailCount);
                    currentBatchCtx.AddProperty("BatchDurationMs", batchStopwatch.ElapsedMilliseconds);
                    currentBatchCtx.AddProperty("ProcessingResult", batchFailCount == 0 ? "Success" : "PartialSuccess");

                    // Log failed items details
                    foreach (var failed in results.Where(r => !r.Success))
                    {
                        using var failedItemCtx = LogCtx.Set();
                        failedItemCtx.AddProperty("ServiceName", nameof(BatchProcessor<T>));
                        failedItemCtx.AddProperty("Operation", "ProcessItem");
                        failedItemCtx.AddProperty("ItemType", typeof(T).Name);
                        failedItemCtx.AddProperty("BatchNumber", batchNumber);
                        failedItemCtx.AddProperty("ItemId", GetItemId(failed.Item));
                        failedItemCtx.AddProperty("ErrorType", failed.Error?.GetType().Name);
                        LogCtx.Logger.Warning("Item processing failed within batch", failed.Error, failedItemCtx);
                    }

                    LogCtx.Logger.Information("Processing batch completed", currentBatchCtx);
                }
                catch (Exception ex)
                {
                    batchStopwatch.Stop();
                    failedCount += batch.Length;
                    
                    currentBatchCtx.AddProperty("BatchDurationMs", batchStopwatch.ElapsedMilliseconds);
                    currentBatchCtx.AddProperty("ProcessingResult", "Failed");
                    currentBatchCtx.AddProperty("ErrorType", ex.GetType().Name);
                    LogCtx.Logger.Error("Entire batch processing failed", ex, currentBatchCtx);
                }

                batchNumber++;
                
                // Progress update
                var progressPercent = (double)batchNumber / totalBatches * 100;
                batchCtx.AddProperty("ProgressPercent", Math.Round(progressPercent, 1));
                batchCtx.AddProperty("ProcessedItems", processedCount);
                batchCtx.AddProperty("FailedItems", failedCount);
                LogCtx.Logger.Information("Batch processing progress update", batchCtx);
            }

            // Final results
            stopwatch.Stop();
            batchCtx.AddProperty("TotalProcessedItems", processedCount);
            batchCtx.AddProperty("TotalFailedItems", failedCount);
            batchCtx.AddProperty("TotalDurationMs", stopwatch.ElapsedMilliseconds);
            batchCtx.AddProperty("ProcessingResult", failedCount == 0 ? "Success" : "PartialSuccess");
            batchCtx.AddProperty("SuccessRate", Math.Round((double)processedCount / itemsList.Count * 100, 2));
            
            LogCtx.Logger.Information("Batch processing operation completed", batchCtx);

            return new BatchResult
            {
                TotalItems = itemsList.Count,
                ProcessedItems = processedCount,
                FailedItems = failedCount,
                Duration = stopwatch.Elapsed,
                Success = failedCount == 0
            };
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            batchCtx.AddProperty("ProcessedItems", processedCount);
            batchCtx.AddProperty("FailedItems", failedCount);
            batchCtx.AddProperty("DurationMs", stopwatch.ElapsedMilliseconds);
            batchCtx.AddProperty("ProcessingResult", "Cancelled");
            LogCtx.Logger.Warning("Batch processing operation was cancelled", batchCtx);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            batchCtx.AddProperty("ProcessedItems", processedCount);
            batchCtx.AddProperty("FailedItems", failedCount);
            batchCtx.AddProperty("DurationMs", stopwatch.ElapsedMilliseconds);
            batchCtx.AddProperty("ProcessingResult", "Failed");
            batchCtx.AddProperty("ErrorType", ex.GetType().Name);
            LogCtx.Logger.Error("Batch processing operation failed", ex, batchCtx);
            throw;
        }
    }

    private static string GetItemId(T item)
    {
        // Try to get ID from common properties
        var type = typeof(T);
        var idProperty = type.GetProperty("Id") ?? type.GetProperty("ID") ?? type.GetProperty("Key");
        return idProperty?.GetValue(item)?.ToString() ?? item?.ToString() ?? "Unknown";
    }
}
```

---

## 📊 **Pattern 5: Performance Monitoring with Detailed Metrics**

```csharp
using NLogShared;   // Required for FailsafeLogger
using LogCtxShared; // Required for LogCtx classes

public class PerformanceTracker : IDisposable
{
    private readonly Stopwatch stopwatch;
    private readonly LogCtx performanceCtx;
    private readonly Dictionary<string, long> checkpoints;
    private readonly Dictionary<string, int> counters;

    public PerformanceTracker(string operationName, params (string Key, object Value)[] initialProperties)
    {
        stopwatch = Stopwatch.StartNew();
        checkpoints = new Dictionary<string, long>();
        counters = new Dictionary<string, int>();
        
        // Create performance tracking context
        performanceCtx = LogCtx.Set(); // ✅ Captures file/line automatically
        performanceCtx.AddProperty("PerformanceOperation", operationName);
        performanceCtx.AddProperty("TrackingId", Guid.NewGuid().ToString());
        performanceCtx.AddProperty("StartTime", DateTime.UtcNow);
        
        // Add initial properties
        foreach (var (key, value) in initialProperties)
        {
            performanceCtx.AddProperty(key, value);
        }
        
        LogCtx.Logger.Information("Performance tracking started", performanceCtx);
    }

    public void AddCheckpoint(string name, params (string Key, object Value)[] additionalProperties)
    {
        var elapsed = stopwatch.ElapsedMilliseconds;
        checkpoints[name] = elapsed;
        
        using var checkpointCtx = LogCtx.Set();
        checkpointCtx.AddProperty("PerformanceCheckpoint", name);
        checkpointCtx.AddProperty("ElapsedMs", elapsed);
        checkpointCtx.AddProperty("TotalCheckpoints", checkpoints.Count);
        
        foreach (var (key, value) in additionalProperties)
        {
            checkpointCtx.AddProperty(key, value);
        }
        
        LogCtx.Logger.Information("Performance checkpoint reached", checkpointCtx);
    }

    public void IncrementCounter(string counterName, int increment = 1)
    {
        if (!counters.ContainsKey(counterName))
        {
            counters[counterName] = 0;
        }
        
        counters[counterName] += increment;
        performanceCtx.AddProperty($"Counter_{counterName}", counters[counterName]);
    }

    public void AddMetric(string metricName, object value)
    {
        performanceCtx.AddProperty($"Metric_{metricName}", value);
    }

    public void Dispose()
    {
        stopwatch.Stop();
        
        // Add final performance metrics
        performanceCtx.AddProperty("TotalDurationMs", stopwatch.ElapsedMilliseconds);
        performanceCtx.AddProperty("EndTime", DateTime.UtcNow);
        performanceCtx.AddProperty("CheckpointCount", checkpoints.Count);
        
        // Add all checkpoints as properties
        foreach (var checkpoint in checkpoints)
        {
            performanceCtx.AddProperty($"Checkpoint_{checkpoint.Key}_Ms", checkpoint.Value);
        }
        
        // Add all final counter values
        foreach (var counter in counters)
        {
            performanceCtx.AddProperty($"FinalCounter_{counter.Key}", counter.Value);
        }
        
        LogCtx.Logger.Information("Performance tracking completed", performanceCtx);
        performanceCtx.Dispose();
    }
}

// Usage example
public class DataExportService
{
    public async Task<ExportResult> ExportDataAsync(ExportRequest request)
    {
        using var tracker = new PerformanceTracker("DataExport",
            ("RequestId", request.Id),
            ("ExportFormat", request.Format.ToString()),
            ("RecordCount", request.Records.Count));

        try
        {
            // Step 1: Data validation
            tracker.AddCheckpoint("ValidationStarted");
            await ValidateDataAsync(request.Records);
            tracker.AddCheckpoint("ValidationCompleted", 
                ("ValidRecords", request.Records.Count(r => r.IsValid)));

            // Step 2: Data transformation
            tracker.AddCheckpoint("TransformationStarted");
            var transformedData = await TransformDataAsync(request.Records);
            tracker.IncrementCounter("TransformedRecords", transformedData.Count);
            tracker.AddCheckpoint("TransformationCompleted",
                ("TransformedRecords", transformedData.Count));

            // Step 3: Export generation
            tracker.AddCheckpoint("ExportStarted");
            var exportResult = await GenerateExportAsync(transformedData, request.Format);
            tracker.AddMetric("ExportSizeBytes", exportResult.SizeBytes);
            tracker.AddCheckpoint("ExportCompleted");

            return exportResult;
        }
        catch (Exception ex)
        {
            tracker.AddMetric("ErrorType", ex.GetType().Name);
            tracker.AddCheckpoint("OperationFailed");
            throw;
        }
    }
}
```

---

## ⚡ **Performance Best Practices Summary**

### **✅ DO:**
1. **Initialize once** per application with `FailsafeLogger.Initialize("NLog.config")`
2. **Always use** `using var ctx = LogCtx.Set()` for proper disposal
3. **Include both** required using statements in every file
4. **Add meaningful properties** that provide operational value
5. **Use structured properties** for SEQ query optimization
6. **Batch logging** for high-frequency operations
7. **Include correlation IDs** for distributed tracing

### **❌ DON'T:**
1. **Never use** `LogCtx.InitLogCtx()` - it doesn't exist!
2. **Never omit** required using statements
3. **Never create** contexts in tight loops without batching
4. **Never forget** to dispose contexts with `using` statements
5. **Never initialize** multiple times in the same process
6. **Never ignore** exception context enrichment

---

**Version:** 0.3.1  
**Last Updated:** October 2025  
**Framework Compatibility:** .NET 8.0+, NLog 6.0.4+, NUnit 4.x+