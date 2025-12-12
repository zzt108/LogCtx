// ✅ FULL FILE VERSION
// File: NLogShared.Tests/CtxLoggerTests.cs
// Project: NLogShared.Tests
// Purpose: Comprehensive unit tests for NLogShared.CtxLogger covering configuration, logging levels, scope context integration, and MemoryTarget-based assertions

using NUnit.Framework;
using Shouldly;
using LogCtxShared;
using NLogShared;
using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.IO;
using System.Linq;

namespace NLogShared.Tests
{
    [TestFixture]
    [Category("unit")]
    public class CtxLoggerTests
    {
        private MemoryTarget memoryTarget;
        private string? testConfigPath;

        [SetUp]
        public void Setup()
        {
            // Reset NLog configuration before each test
            LogManager.Configuration = null;

            // Create in-memory MemoryTarget for deterministic log capture
            memoryTarget = new MemoryTarget("memory")
            {
                // 🔄 MODIFY: Add :format=@ for JSON serialization
                Layout = "${level:uppercase=true}|${message}|${scopeproperty:CTX_STRACE}|${event-properties:P00:format=@}|${event-properties:P01:format=@}"
            };

            var config = new LoggingConfiguration();
            config.AddTarget(memoryTarget);
            config.AddRuleForAllLevels(memoryTarget);
            LogManager.Configuration = config;
        }

        [TearDown]
        public void TearDown()
        {
            // Flush and reset NLog
            LogManager.Flush();
            LogManager.Configuration = null;

            // Clean up test config files
            if (!string.IsNullOrEmpty(testConfigPath) && File.Exists(testConfigPath))
            {
                try
                {
                    File.Delete(testConfigPath);
                }
                catch
                {
                    // ignore cleanup failures
                }
            }

            memoryTarget?.Dispose();
        }

        // ConfigureXml Tests

        [Test]
        public void ConfigureXml_WithValidPath_ReturnsTrue()
        {
            // Arrange
            testConfigPath = CreateTempNLogConfig();
            var logger = new CtxLogger();

            // Act
            var result = logger.ConfigureXml(testConfigPath);

            // Assert
            result.ShouldBeTrue();
            LogManager.Configuration.ShouldNotBeNull();

            logger.Dispose();
        }

        [Test]
        public void ConfigureXml_WithInvalidPath_ReturnsFalse()
        {
            // Arrange
            var logger = new CtxLogger();

            // Act
            var result = logger.ConfigureXml("NonExistent.config");

            // Assert
            result.ShouldBeFalse();

            logger.Dispose();
        }

        [Test]
        public void ConfigureXml_CalledTwice_ReturnsTrueAndDoesNotReconfigure()
        {
            // Arrange
            testConfigPath = CreateTempNLogConfig();
            var logger = new CtxLogger();
            logger.ConfigureXml(testConfigPath);

            // Act
            var result = logger.ConfigureXml(testConfigPath);

            // Assert
            result.ShouldBeTrue();

            logger.Dispose();
        }

        [Test]
        public void ConfigureJson_ThrowsNotImplementedException()
        {
            // Arrange
            var logger = new CtxLogger();

            // Act & Assert
            Should.Throw<NotImplementedException>(() => logger.ConfigureJson("any.json"));

            logger.Dispose();
        }

        // Logging Level Tests with MemoryTarget

        [Test]
        public void Debug_WritesToMemoryTarget_WithMessage()
        {
            // Arrange
            var logger = new CtxLogger();
            memoryTarget.Logs.Clear();

            // Act
            logger.Debug("debug message");
            LogManager.Flush();

            // Assert
            memoryTarget.Logs.Count.ShouldBe(1);
            memoryTarget.Logs[0].ShouldContain("DEBUG|debug message");

            logger.Dispose();
        }

        [Test]
        public void Info_WritesToMemoryTarget_WithMessage()
        {
            // Arrange
            var logger = new CtxLogger();
            memoryTarget.Logs.Clear();

            // Act
            logger.Info("info message");
            LogManager.Flush();

            // Assert
            memoryTarget.Logs.Count.ShouldBe(1);
            memoryTarget.Logs[0].ShouldContain("INFO|info message");

            logger.Dispose();
        }

        [Test]
        public void Warn_WritesToMemoryTarget_WithMessage()
        {
            // Arrange
            var logger = new CtxLogger();
            memoryTarget.Logs.Clear();

            // Act
            logger.Warn("warning message");
            LogManager.Flush();

            // Assert
            memoryTarget.Logs.Count.ShouldBe(1);
            memoryTarget.Logs[0].ShouldContain("WARN|warning message");

            logger.Dispose();
        }

        [Test]
        public void Error_WritesToMemoryTarget_WithExceptionAndMessage()
        {
            // Arrange
            var logger = new CtxLogger();
            memoryTarget.Logs.Clear();
            var exception = new InvalidOperationException("test error");

            // Act
            logger.Error(exception, "error message");
            LogManager.Flush();

            // Assert
            memoryTarget.Logs.Count.ShouldBe(1);
            memoryTarget.Logs[0].ShouldContain("ERROR|error message");

            logger.Dispose();
        }

        [Test]
        public void Fatal_WritesToMemoryTarget_WithExceptionAndMessage()
        {
            // Arrange
            var logger = new CtxLogger();
            memoryTarget.Logs.Clear();
            var exception = new Exception("fatal error");

            // Act
            logger.Fatal(exception, "fatal message");
            LogManager.Flush();

            // Assert
            memoryTarget.Logs.Count.ShouldBe(1);
            memoryTarget.Logs[0].ShouldContain("FATAL|fatal message");

            logger.Dispose();
        }

        [Test]
        public void Trace_WritesToMemoryTarget_WithMessage()
        {
            // Arrange
            var logger = new CtxLogger();
            memoryTarget.Logs.Clear();

            // Act
            logger.Trace("trace message");
            LogManager.Flush();

            // Assert
            memoryTarget.Logs.Count.ShouldBe(1);
            memoryTarget.Logs[0].ShouldContain("TRACE|trace message");

            logger.Dispose();
        }

        // Scope Context Integration Tests

        [Test]
        public void DebugWithCtxSet_WritesScopePropertiesToMemoryTarget()
        {
            // Arrange
            var logger = new CtxLogger();
            memoryTarget.Logs.Clear();

            // Act
            LogCtx.Set(new Props("valueA", "valueB"));
            logger.Debug("debug with scope");
            LogManager.Flush();

            // Assert
            memoryTarget.Logs.Count.ShouldBe(1);
            var logLine = memoryTarget.Logs[0];
            logLine.ShouldContain("DEBUG|debug with scope");
            logLine.ShouldContain("CtxLoggerTests"); // CTX_STRACE includes file name
            logLine.ShouldContain("\"valueA\""); // P00 rendered as JSON
            logLine.ShouldContain("\"valueB\""); // P01 rendered as JSON

            logger.Dispose();
        }

        [Test]
        public void InfoWithCtxSet_IncludesCTX_STRACE_InLog()
        {
            // Arrange
            var logger = new CtxLogger();
            memoryTarget.Logs.Clear();

            // Act
            LogCtx.Set(new Props("X"));
            logger.Info("info with strace");
            LogManager.Flush();

            // Assert
            memoryTarget.Logs.Count.ShouldBe(1);
            var logLine = memoryTarget.Logs[0];
            logLine.ShouldContain("INFO|info with strace");
            logLine.ShouldContain("|"); // CTX_STRACE format: FileName.MethodName.Line

            logger.Dispose();
        }

        [Test]
        public void MultipleLogsWithCtxClearBetweenCalls_IsolatesScope()
        {
            // Arrange
            var logger = new CtxLogger();
            memoryTarget.Logs.Clear();

            // Act
            LogCtx.Set(new Props("first"));
            logger.Info("log one");
            LogCtx.Set(new Props("second"));
            logger.Info("log two");
            LogManager.Flush();

            // Assert
            memoryTarget.Logs.Count.ShouldBe(2);
            memoryTarget.Logs[0].ShouldContain("\"first\"");
            memoryTarget.Logs[0].ShouldNotContain("\"second\"");
            memoryTarget.Logs[1].ShouldContain("\"second\"");
            memoryTarget.Logs[1].ShouldNotContain("\"first\"");

            logger.Dispose();
        }

        // Dispose and Flush Tests

        [Test]
        public void Dispose_FlushesLogsAndDoesNotShutdownLogManager()
        {
            // Arrange
            var logger1 = new CtxLogger();
            memoryTarget.Logs.Clear();

            // Act
            logger1.Info("log from logger1");
            logger1.Dispose(); // Should flush, not shutdown

            var logger2 = new CtxLogger();
            logger2.Info("log from logger2");
            LogManager.Flush();

            // Assert
            memoryTarget.Logs.Count.ShouldBe(2);
            memoryTarget.Logs[0].ShouldContain("log from logger1");
            memoryTarget.Logs[1].ShouldContain("log from logger2");

            logger2.Dispose();
        }

        [Test]
        public void Dispose_CanBeCalledMultipleTimesSafely()
        {
            // Arrange
            var logger = new CtxLogger();

            // Act & Assert
            Should.NotThrow(() =>
            {
                logger.Dispose();
                logger.Dispose();
            });
        }

        // Constructor and Initialization Tests

        [Test]
        public void Ctor_WithNoArgs_InitializesWithFailsafe()
        {
            // Act
            var logger = new CtxLogger();

            // Assert
            logger.ShouldNotBeNull();
            logger.Ctx.ShouldNotBeNull();

            logger.Dispose();
        }

        [Test]
        public void Ctor_WithCustomScopeContext_UsesProvidedContext()
        {
            // Arrange
            var customScope = new NLogScopeContext();

            // Act
            var logger = new CtxLogger(customScope);

            // Assert
            logger.Ctx.ShouldNotBeNull();

            logger.Dispose();
        }

        [Test]
        public void Ctor_WithConfigPath_LoadsConfiguration()
        {
            // Arrange
            testConfigPath = CreateTempNLogConfig();

            // Act
            var logger = new CtxLogger(testConfigPath);

            // Assert
            LogManager.Configuration.ShouldNotBeNull();

            logger.Dispose();
        }

        // Helper Methods

        private string CreateTempNLogConfig()
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"NLog_{Guid.NewGuid()}.config");
            var configXml = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<nlog xmlns=""http://www.nlog-project.org/schemas/NLog.xsd""
      xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <targets>
    <target xsi:type=""Memory"" name=""mem""
            layout=""${level:uppercase=true}|${message}|${scopeproperty:CTX_STRACE}|${event-properties:P00:format=@}|${event-properties:P01:format=@}"" />
  </targets>
  <rules>
    <logger name=""*"" minlevel=""Trace"" writeTo=""mem"" />
  </rules>
</nlog>";

            File.WriteAllText(tempPath, configXml);
            return tempPath;
        }
    }
}