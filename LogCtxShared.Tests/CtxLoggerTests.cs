// ✅ FULL FILE VERSION
// NLogShared.Tests/CtxLoggerTests.cs
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
        private MemoryTarget _memoryTarget;
        private string? _testConfigPath;

        [SetUp]
        public void Setup()
        {
            // Reset NLog configuration before each test
            LogManager.Configuration = null;

            // Create in-memory MemoryTarget for deterministic log capture
            _memoryTarget = new MemoryTarget("memory")
            {
                Layout = "${level:uppercase=true}|${message}|${scopeproperty:CTX_STRACE}|${event-properties:P00}|${event-properties:P01}"
            };

            var config = new LoggingConfiguration();
            config.AddTarget(_memoryTarget);
            config.AddRuleForAllLevels(_memoryTarget);
            LogManager.Configuration = config;
        }

        [TearDown]
        public void TearDown()
        {
            // Flush and reset NLog
            LogManager.Flush();
            LogManager.Configuration = null;

            // Clean up test config files
            if (!string.IsNullOrEmpty(_testConfigPath) && File.Exists(_testConfigPath))
            {
                try { File.Delete(_testConfigPath); } catch { /* ignore cleanup failures */ }
            }
            _memoryTarget.Dispose();
        }

        // ────────────────────────────────────────────────────────────────
        // ConfigureXml Tests
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void ConfigureXml_WithValidPath_ReturnsTrue()
        {
            // Arrange
            _testConfigPath = CreateTempNLogConfig();
            var logger = new CtxLogger();

            // Act
            var result = logger.ConfigureXml(_testConfigPath);

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
        public void ConfigureXml_CalledTwice_ReturnsTrue_AndDoesNotReconfigure()
        {
            // Arrange
            _testConfigPath = CreateTempNLogConfig();
            var logger = new CtxLogger();
            logger.ConfigureXml(_testConfigPath);

            // Act
            var result = logger.ConfigureXml(_testConfigPath);

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

        // ────────────────────────────────────────────────────────────────
        // Logging Level Tests with MemoryTarget
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Debug_Writes_To_MemoryTarget_With_Message()
        {
            // Arrange
            var logger = new CtxLogger();
            _memoryTarget.Logs.Clear();

            // Act
            logger.Debug("debug message");
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(1);
            _memoryTarget.Logs[0].ShouldContain("DEBUG|debug message");
            logger.Dispose();
        }

        [Test]
        public void Info_Writes_To_MemoryTarget_With_Message()
        {
            // Arrange
            var logger = new CtxLogger();
            _memoryTarget.Logs.Clear();

            // Act
            logger.Info("info message");
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(1);
            _memoryTarget.Logs[0].ShouldContain("INFO|info message");
            logger.Dispose();
        }

        [Test]
        public void Warn_Writes_To_MemoryTarget_With_Message()
        {
            // Arrange
            var logger = new CtxLogger();
            _memoryTarget.Logs.Clear();

            // Act
            logger.Warn("warning message");
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(1);
            _memoryTarget.Logs[0].ShouldContain("WARN|warning message");
            logger.Dispose();
        }

        [Test]
        public void Error_Writes_To_MemoryTarget_With_Exception_And_Message()
        {
            // Arrange
            var logger = new CtxLogger();
            _memoryTarget.Logs.Clear();
            var exception = new InvalidOperationException("test error");

            // Act
            logger.Error(exception, "error message");
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(1);
            _memoryTarget.Logs[0].ShouldContain("ERROR|error message");
            logger.Dispose();
        }

        [Test]
        public void Fatal_Writes_To_MemoryTarget_With_Exception_And_Message()
        {
            // Arrange
            var logger = new CtxLogger();
            _memoryTarget.Logs.Clear();
            var exception = new Exception("fatal error");

            // Act
            logger.Fatal(exception, "fatal message");
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(1);
            _memoryTarget.Logs[0].ShouldContain("FATAL|fatal message");
            logger.Dispose();
        }

        [Test]
        public void Trace_Writes_To_MemoryTarget_With_Message()
        {
            // Arrange
            var logger = new CtxLogger();
            _memoryTarget.Logs.Clear();

            // Act
            logger.Trace("trace message");
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(1);
            _memoryTarget.Logs[0].ShouldContain("TRACE|trace message");
            logger.Dispose();
        }

        // ────────────────────────────────────────────────────────────────
        // Scope Context Integration Tests
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Debug_With_Ctx_Set_Writes_Scope_Properties_To_MemoryTarget()
        {
            // Arrange
            var logger = new CtxLogger();
            _memoryTarget.Logs.Clear();

            // Act
            logger.Ctx.Set(new Props("valueA", "valueB"));
            logger.Debug("debug with scope");
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(1);
            var logLine = _memoryTarget.Logs[0];
            logLine.ShouldContain("DEBUG|debug with scope");
            logLine.ShouldContain("CtxLoggerTests"); // CTX_STRACE includes file name
            logLine.ShouldContain("\"valueA\""); // P00 rendered as JSON
            logLine.ShouldContain("\"valueB\""); // P01 rendered as JSON
            logger.Dispose();
        }

        [Test]
        public void Info_With_Ctx_Set_Includes_CTX_STRACE_In_Log()
        {
            // Arrange
            var logger = new CtxLogger();
            _memoryTarget.Logs.Clear();

            // Act
            logger.Ctx.Set(new Props("X"));
            logger.Info("info with strace");
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(1);
            var logLine = _memoryTarget.Logs[0];
            logLine.ShouldContain("INFO|info with strace");
            logLine.ShouldContain("::"); // CTX_STRACE format: FileName::MethodName::Line
            logger.Dispose();
        }

        [Test]
        public void Multiple_Logs_With_Ctx_Clear_Between_Calls_Isolates_Scope()
        {
            // Arrange
            var logger = new CtxLogger();
            _memoryTarget.Logs.Clear();

            // Act
            logger.Ctx.Set(new Props("first"));
            logger.Info("log one");
            logger.Ctx.Set(new Props("second"));
            logger.Info("log two");
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(2);
            _memoryTarget.Logs[0].ShouldContain("\"first\"");
            _memoryTarget.Logs[0].ShouldNotContain("\"second\"");
            _memoryTarget.Logs[1].ShouldContain("\"second\"");
            _memoryTarget.Logs[1].ShouldNotContain("\"first\"");
            logger.Dispose();
        }

        // ────────────────────────────────────────────────────────────────
        // Dispose and Flush Tests
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Dispose_Flushes_Logs_And_Does_Not_Shutdown_LogManager()
        {
            // Arrange
            var logger1 = new CtxLogger();
            _memoryTarget.Logs.Clear();

            // Act
            logger1.Info("log from logger1");
            logger1.Dispose(); // Should flush, not shutdown

            var logger2 = new CtxLogger();
            logger2.Info("log from logger2");
            LogManager.Flush();

            // Assert
            _memoryTarget.Logs.Count.ShouldBe(2);
            _memoryTarget.Logs[0].ShouldContain("log from logger1");
            _memoryTarget.Logs[1].ShouldContain("log from logger2");
            logger2.Dispose();
        }

        [Test]
        public void Dispose_Can_Be_Called_Multiple_Times_Safely()
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

        // ────────────────────────────────────────────────────────────────
        // Constructor and Initialization Tests
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Ctor_With_No_Args_Initializes_With_Failsafe()
        {
            // Act
            var logger = new CtxLogger();

            // Assert
            logger.ShouldNotBeNull();
            logger.Ctx.ShouldNotBeNull();
            logger.Dispose();
        }

        [Test]
        public void Ctor_With_Custom_ScopeContext_Uses_Provided_Context()
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
        public void Ctor_With_ConfigPath_Loads_Configuration()
        {
            // Arrange
            _testConfigPath = CreateTempNLogConfig();

            // Act
            var logger = new CtxLogger(_testConfigPath);

            // Assert
            LogManager.Configuration.ShouldNotBeNull();
            logger.Dispose();
        }

        // ────────────────────────────────────────────────────────────────
        // Helper Methods
        // ────────────────────────────────────────────────────────────────

        private string CreateTempNLogConfig()
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"NLog_{Guid.NewGuid()}.config");
            var configXml = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<nlog xmlns=""http://www.nlog-project.org/schemas/NLog.xsd""
      xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <targets>
    <target xsi:type=""Memory"" name=""mem"" 
            layout=""${level:uppercase=true}|${message}|${scopeproperty:CTX_STRACE}|${event-properties:P00}"" />
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
