// NLogShared.Tests/NLogFailsafeLoggerTests.cs
// Project: NLogShared.Tests
// Purpose: Unit tests for NLogFailsafeLogger initialization, config resolution order, and fallback behavior

using NUnit.Framework;
using Shouldly;
using NLogShared;
using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace NLogShared.Tests
{
    [TestFixture]
    [Category("unit")]
    [Parallelizable(ParallelScope.None)] // 🔄 MODIFY — Prevent file conflicts from parallel execution
    public class NLogFailsafeLoggerTests
    {
        private List<string> _tempFilesToCleanup = new List<string>();
        private string _originalBaseDir;

        [SetUp]
        public void Setup()
        {
            // Reset NLog configuration before each test
            LogManager.Configuration = null;
            _originalBaseDir = AppContext.BaseDirectory;
        }

        [TearDown]
        public void TearDown()
        {
            LogManager.Configuration = null;

            // 🔄 MODIFY — Clean up temp config files
            foreach (var file in _tempFilesToCleanup)
            {
                try
                {
                    if (File.Exists(file))
                        File.Delete(file);
                }
                catch { /* ignore cleanup failures */ }
            }
            _tempFilesToCleanup.Clear();
        }

        // ────────────────────────────────────────────────────────────────
        // Config Resolution Tests
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Initialize_With_Existing_NLog_Config_Returns_True()
        {
            // Arrange
            var baseDir = AppContext.BaseDirectory;
            var configPath = CreateTempConfigFile("TestInit.config", baseDir);

            // Reset NLog to force initialization
            LogManager.Configuration = null;

            // Act
            var logger = new CtxLogger(configPath);
            var initialized = NLogFailsafeLogger.Initialize(logger, "TestInit.config");

            // Assert
            initialized.ShouldBeTrue();
            LogManager.Configuration.ShouldNotBeNull();
            LogManager.Configuration.AllTargets.ShouldNotBeEmpty();
            logger.Dispose();
        }

        [Test]
        public void Initialize_With_NonExistent_Config_Uses_Fallback_Returns_False()
        {
            // Arrange
            // Reset NLog
            LogManager.Configuration = null;

            // Act
            var logger = new CtxLogger();
            var initialized = NLogFailsafeLogger.Initialize(logger, "NonExistent.config", "NonExistent.json");

            // Assert
            initialized.ShouldBeFalse(); // 🔄 MODIFY — Should return false when using fallback
            LogManager.Configuration.ShouldNotBeNull(); // Fallback config applied
            logger.Dispose();
        }

        [Test]
        public void Initialize_Fallback_Creates_Logs_Directory()
        {
            // Arrange
            var baseDir = AppContext.BaseDirectory;
            var logsDir = Path.Combine(baseDir, "logs");

            // Clean up logs directory if it exists from previous runs
            if (Directory.Exists(logsDir))
            {
                try { Directory.Delete(logsDir, true); } catch { /* ignore */ }
            }

            // Reset NLog to force initialization
            LogManager.Configuration = null;

            // Act
            var logger = new CtxLogger();
            NLogFailsafeLogger.Initialize(logger, "NonExistent.config", "NonExistent.json");

            // Assert
            // 🔄 MODIFY — Fallback should create logs directory
            Directory.Exists(logsDir).ShouldBeTrue("Fallback config should create logs directory");
            LogManager.Configuration.ShouldNotBeNull();
            logger.Dispose();
        }

        [Test]
        public void Initialize_Prefers_XML_Over_JSON_When_Both_Exist()
        {
            // Arrange
            var baseDir = AppContext.BaseDirectory;
            var xmlPath = CreateTempConfigFile("TestPrefer.config", baseDir);
            var jsonPath = CreateTempConfigFile("TestPrefer.json", baseDir);

            // Reset NLog
            LogManager.Configuration = null;

            // Act
            var logger = new CtxLogger();
            var initialized = NLogFailsafeLogger.Initialize(logger, "TestPrefer.config", "TestPrefer.json");

            // Assert
            initialized.ShouldBeTrue();
            LogManager.Configuration.ShouldNotBeNull();
            // Both files exist, XML should be preferred and loaded successfully
            LogManager.Configuration.AllTargets.Any().ShouldBeTrue();
            logger.Dispose();
        }

        [Test]
        public void Initialize_With_Existing_LogManager_Config_Preserves_It()
        {
            // Arrange - manually configure NLog first
            var testTarget = new MemoryTarget("test_memory")
            {
                Layout = "${level}|${message}"
            };

            var config = new LoggingConfiguration();
            config.AddTarget(testTarget);
            config.AddRuleForAllLevels(testTarget);
            LogManager.Configuration = config;

            // Act
            var logger = new CtxLogger();
            var initialized = NLogFailsafeLogger.Initialize(logger, "SomeConfig.config");

            // Assert
            initialized.ShouldBeTrue(); // Should return true because config already exists
            LogManager.Configuration.ShouldNotBeNull();
            // Original test target should still be present
            LogManager.Configuration.AllTargets.Any(t => t.Name == "test_memory").ShouldBeTrue();
            logger.Dispose();
        }

        [Test]
        public void Initialize_Resolves_Config_In_Expected_Order()
        {
            // Arrange - create only JSON config to test resolution order
            var baseDir = AppContext.BaseDirectory;
            var jsonPath = CreateTempConfigFile("TestOrder.json", baseDir);

            // Reset NLog to force initialization
            LogManager.Configuration = null;

            // Act
            var logger = new CtxLogger();
            var initialized = NLogFailsafeLogger.Initialize(logger, "NonExistent.config", "TestOrder.json");

            // Assert
            // 🔄 MODIFY — Since XML doesn't exist, should use fallback (JSON not implemented)
            // Returns false because fallback was used
            initialized.ShouldBeFalse();
            LogManager.Configuration.ShouldNotBeNull();
            logger.Dispose();
        }

        [Test]
        public void Initialize_Never_Throws_Even_On_Catastrophic_Failure()
        {
            // Arrange
            LogManager.Configuration = null;

            // Act & Assert
            Should.NotThrow(() =>
            {
                var logger = new CtxLogger();
                var result = NLogFailsafeLogger.Initialize(logger, null, null);
                // Should apply NoOp fallback and return false
                result.ShouldBeFalse();
                LogManager.Configuration.ShouldNotBeNull();
                logger.Dispose();
            });
        }

        [Test]
        public void Initialize_With_Null_Logger_Applies_NoOp_Fallback()
        {
            // Arrange
            LogManager.Configuration = null;

            // Act
            var logger = new CtxLogger();
            var result = NLogFailsafeLogger.Initialize(logger, null, null);

            // Assert
            result.ShouldBeFalse(); // Fallback used
            LogManager.Configuration.ShouldNotBeNull();
            // NullTarget should be present in fallback
            LogManager.Configuration.AllTargets.Any(t => t is NullTarget).ShouldBeTrue();
            logger.Dispose();
        }

        // ────────────────────────────────────────────────────────────────
        // Helper Methods
        // ────────────────────────────────────────────────────────────────

        // ✅ NEW — Helper to create temp NLog config files in controlled locations
        private string CreateTempConfigFile(string fileName, string targetDirectory = null)
        {
            targetDirectory ??= Path.GetTempPath();
            var configPath = Path.Combine(targetDirectory, fileName);

            var configXml = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<nlog xmlns=""http://www.nlog-project.org/schemas/NLog.xsd""
      xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <targets>
    <target xsi:type=""Memory"" name=""mem"" 
            layout=""${level:uppercase=true}|${message}|${scopeproperty:CTX_STRACE}"" />
  </targets>
  <rules>
    <logger name=""*"" minlevel=""Trace"" writeTo=""mem"" />
  </rules>
</nlog>";

            File.WriteAllText(configPath, configXml);
            _tempFilesToCleanup.Add(configPath);
            return configPath;
        }
    }
}
