// ✅ FULL FILE VERSION
// File: NLogShared.Tests/ConfigResolutionTests.cs
// Project: NLogShared.Tests
// Purpose: Tests for NLog configuration resolution from XML/JSON files, verifying layout and target setup

using NUnit.Framework;
using Shouldly;
using LogCtxShared;
using NLogShared;
using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.IO;

namespace NLogShared.Tests
{
    [TestFixture]
    [Category("unit")]
    public class ConfigResolutionTests
    {
        private MemoryTarget? memoryTarget;
        private string? testConfigPath;

        [SetUp]
        public void Setup()
        {
            // Reset NLog configuration before each test
            LogManager.Configuration = null;
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

        [Test]
        public void ConfigureXml_LoadsMemoryTargetFromFile()
        {
            // Arrange
            testConfigPath = CreateTempNLogConfig();
            var logger = new CtxLogger();

            // Act
            var result = logger.ConfigureXml(testConfigPath);
            LogManager.Flush();

            // Assert
            result.ShouldBeTrue();
            LogManager.Configuration.ShouldNotBeNull();

            // Verify MemoryTarget was loaded from config
            var targets = LogManager.Configuration.AllTargets;
            targets.ShouldContain(t => t is MemoryTarget && t.Name == "mem");

            logger.Dispose();
        }

        [Test]
        public void ConfigureXml_WithEventProperties_RendersJsonValues()
        {
            // Arrange
            testConfigPath = CreateTempNLogConfig();
            var logger = new CtxLogger(testConfigPath);

            // Extract MemoryTarget from loaded config
            memoryTarget = LogManager.Configuration.FindTargetByName<MemoryTarget>("mem");
            memoryTarget.ShouldNotBeNull();

            // Act
            LogCtx.Set(new Props("valueA", "valueB"));
            logger.Info("test with props");
            LogManager.Flush();

            // Assert
            memoryTarget.Logs.Count.ShouldBe(1);
            var logLine = memoryTarget.Logs[0];
            logLine.ShouldContain("INFO|test with props");
            logLine.ShouldContain("\"valueA\""); // P00 rendered as JSON
            logLine.ShouldContain("\"valueB\""); // P01 rendered as JSON

            logger.Dispose();
        }

        [Test]
        public void ConfigureXml_WithInvalidPath_ReturnsFalse()
        {
            // Arrange
            var logger = new CtxLogger();

            // Act
            var result = logger.ConfigureXml("NonExistent_Config.xml");

            // Assert
            result.ShouldBeFalse();

            logger.Dispose();
        }

        [Test]
        public void ConfigureXml_WithRelativePath_ResolvesFromBaseDirectory()
        {
            // Arrange
            var baseDir = AppContext.BaseDirectory;
            testConfigPath = Path.Combine(baseDir, $"TestNLog_{Guid.NewGuid()}.config");

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

            File.WriteAllText(testConfigPath, configXml);

            var relativeFileName = Path.GetFileName(testConfigPath);
            var logger = new CtxLogger();

            // Act
            var result = logger.ConfigureXml(relativeFileName);

            // Assert
            result.ShouldBeTrue();
            LogManager.Configuration.ShouldNotBeNull();

            logger.Dispose();
        }

        [Test]
        public void ConfigureXml_CalledTwice_DoesNotReconfigure()
        {
            // Arrange
            testConfigPath = CreateTempNLogConfig();
            var logger = new CtxLogger();

            logger.ConfigureXml(testConfigPath);
            var firstConfig = LogManager.Configuration;

            // Act
            var result = logger.ConfigureXml(testConfigPath);
            var secondConfig = LogManager.Configuration;

            // Assert
            result.ShouldBeTrue();
            ReferenceEquals(firstConfig, secondConfig).ShouldBeTrue(); // Same config instance

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
    <!-- 🔄 MODIFY: Add :format=@ for JSON serialization -->
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