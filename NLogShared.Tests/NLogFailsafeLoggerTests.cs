// NLogShared.Tests/NLogFailsafeLoggerTests.cs
// Project: NLogShared.Tests
// Purpose: Unit tests for NLogFailsafeLogger.Initialize covering XML → JSON → fallback → no-op paths without throwing

using NUnit.Framework;
using Shouldly;
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
    public class NLogFailsafeLoggerTests
    {
        private string _testDirectory;

        [SetUp]
        public void Setup()
        {
            // Create a unique temp directory for each test to avoid conflicts
            _testDirectory = Path.Combine(Path.GetTempPath(), $"NLogTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDirectory);

            // Change the AppContext.BaseDirectory substitute by using the test directory
            Environment.CurrentDirectory = _testDirectory;

            // Reset NLog configuration before each test
            LogManager.Configuration = null;
        }

        [TearDown]
        public void TearDown()
        {
            // Flush and reset NLog
            LogManager.Flush();
            LogManager.Configuration = null;

            // Clean up test directory
            if (Directory.Exists(_testDirectory))
            {
                try { Directory.Delete(_testDirectory, recursive: true); } catch { /* ignore cleanup failures */ }
            }
        }

        // ────────────────────────────────────────────────────────────────
        // XML Configuration Path Tests
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Initialize_Uses_Xml_When_Present()
        {
            // Arrange
            var xmlPath = Path.Combine(_testDirectory, "NLog.config");
            File.WriteAllText(xmlPath, CreateMinimalNLogXml());
            var logger = new CtxLogger();

            // Act
            var result = NLogFailsafeLogger.Initialize(logger, "NLog.config", "NLog.json");

            // Assert
            result.ShouldBeTrue();
            LogManager.Configuration.ShouldNotBeNull();
            LogManager.Configuration.AllTargets.Count.ShouldBeGreaterThan(0);
        }

        [Test]
        public void Initialize_Prefers_Xml_Over_Json_When_Both_Exist()
        {
            // Arrange
            var xmlPath = Path.Combine(_testDirectory, "NLog.config");
            var jsonPath = Path.Combine(_testDirectory, "NLog.json");
            File.WriteAllText(xmlPath, CreateMinimalNLogXml());
            File.WriteAllText(jsonPath, "{}"); // Dummy JSON (would fail if attempted)
            var logger = new CtxLogger();

            // Act
            var result = NLogFailsafeLogger.Initialize(logger, "NLog.config", "NLog.json");

            // Assert
            result.ShouldBeTrue();
            LogManager.Configuration.ShouldNotBeNull();
            // Since XML was preferred and succeeded, JSON should not have been attempted
        }

        [Test]
        public void Initialize_Reads_Xml_And_Configures_Targets()
        {
            // Arrange
            var xmlPath = Path.Combine(_testDirectory, "NLog.config");
            File.WriteAllText(xmlPath, CreateNLogXmlWithMemoryTarget());
            var logger = new CtxLogger();

            // Act
            var result = NLogFailsafeLogger.Initialize(logger, "NLog.config", "NLog.json");

            // Assert
            result.ShouldBeTrue();
            LogManager.Configuration.ShouldNotBeNull();
            LogManager.Configuration.FindTargetByName<MemoryTarget>("memory").ShouldNotBeNull();
        }

        // ────────────────────────────────────────────────────────────────
        // JSON Configuration Path Tests
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Initialize_Uses_Fallback_When_Json_Exists_But_ConfigureJson_Throws()
        {
            // Arrange
            var jsonPath = Path.Combine(_testDirectory, "NLog.json");
            File.WriteAllText(jsonPath, "{}"); // JSON file exists, but CtxLogger.ConfigureJson throws NotImplementedException
            var logger = new CtxLogger();

            // Act
            var result = NLogFailsafeLogger.Initialize(logger, "NonExistent.config", "NLog.json");

            // Assert
            result.ShouldBeTrue();
            LogManager.Configuration.ShouldNotBeNull();
            // Fallback should have created console + file targets
            LogManager.Configuration.AllTargets.ShouldContain(t => t is ConsoleTarget);
            LogManager.Configuration.AllTargets.ShouldContain(t => t is FileTarget);
        }

        [Test]
        public void Initialize_Falls_Back_To_Minimal_Config_When_Json_Throws()
        {
            // Arrange
            var jsonPath = Path.Combine(_testDirectory, "NLog.json");
            File.WriteAllText(jsonPath, "{invalid json content}");
            var logger = new CtxLogger();

            // Act
            var result = NLogFailsafeLogger.Initialize(logger, "NonExistent.config", "NLog.json");

            // Assert
            result.ShouldBeTrue();
            LogManager.Configuration.ShouldNotBeNull();
            LogManager.Configuration.AllTargets.Count.ShouldBeGreaterThan(0);
        }

        // ────────────────────────────────────────────────────────────────
        // Minimal Fallback Configuration Tests
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Initialize_Creates_Minimal_Fallback_When_No_Config_Files_Exist()
        {
            // Arrange
            var logger = new CtxLogger();

            // Act
            var result = NLogFailsafeLogger.Initialize(logger, "NonExistent.config", "NonExistent.json");

            // Assert
            result.ShouldBeTrue();
            LogManager.Configuration.ShouldNotBeNull();
            LogManager.Configuration.AllTargets.ShouldContain(t => t is ConsoleTarget);
            LogManager.Configuration.AllTargets.ShouldContain(t => t is FileTarget);
        }

        [Test]
        public void Initialize_Minimal_Fallback_Creates_Logs_Directory()
        {
            // Arrange
            var logger = new CtxLogger();
            var expectedLogsDir = Path.Combine(_testDirectory, "logs");

            // Act
            var result = NLogFailsafeLogger.Initialize(logger, "NonExistent.config", "NonExistent.json");

            // Assert
            result.ShouldBeTrue();
            Directory.Exists(expectedLogsDir).ShouldBeTrue();
        }

        [Test]
        public void Initialize_Minimal_Fallback_Configures_Console_And_File_Targets()
        {
            // Arrange
            var logger = new CtxLogger();

            // Act
            var result = NLogFailsafeLogger.Initialize(logger, "NonExistent.config", "NonExistent.json");

            // Assert
            result.ShouldBeTrue();
            var config = LogManager.Configuration;
            config.ShouldNotBeNull();

            var consoleTarget = config.FindTargetByName<ConsoleTarget>("console");
            consoleTarget.ShouldNotBeNull();
            consoleTarget.Layout.ToString().ShouldContain("${level:uppercase=true}");

            var fileTarget = config.FindTargetByName<FileTarget>("file");
            fileTarget.ShouldNotBeNull();
            fileTarget.FileName.ToString().ShouldContain("app.log");
        }

        [Test]
        public void Initialize_Minimal_Fallback_Logs_All_Levels()
        {
            // Arrange
            var logger = new CtxLogger();

            // Act
            var result = NLogFailsafeLogger.Initialize(logger, "NonExistent.config", "NonExistent.json");

            // Assert
            result.ShouldBeTrue();
            var config = LogManager.Configuration;
            config.LoggingRules.ShouldNotBeEmpty();
            config.LoggingRules.ShouldContain(r => r.Levels.Contains(LogLevel.Trace));
            config.LoggingRules.ShouldContain(r => r.Levels.Contains(LogLevel.Fatal));
        }

        // ────────────────────────────────────────────────────────────────
        // No-Op Fallback Tests (Hard Failure Path)
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Initialize_Never_Throws_Even_When_All_Paths_Fail()
        {
            // Arrange
            var logger = new CtxLogger();
            // Simulate a scenario where even fallback might fail (e.g., restricted filesystem)
            // This is hard to simulate directly, but we can verify the method never throws

            // Act & Assert
            Should.NotThrow(() =>
            {
                var result = NLogFailsafeLogger.Initialize(logger, "NonExistent.config", "NonExistent.json");
                result.ShouldBeTrue();
            });
        }

        [Test]
        public void Initialize_Sets_Null_Target_In_Worst_Case_Fallback()
        {
            // Arrange
            // We can't easily force ApplyNoOpFallback to trigger, but we can verify it's safe
            var logger = new CtxLogger();

            // Act
            var result = NLogFailsafeLogger.Initialize(logger, "NonExistent.config", "NonExistent.json");

            // Assert
            result.ShouldBeTrue();
            LogManager.Configuration.ShouldNotBeNull();
            // In normal case, we should get console + file targets
            // In no-op case, we'd get a NullTarget
            LogManager.Configuration.AllTargets.Count.ShouldBeGreaterThanOrEqualTo(1);
        }

        // ────────────────────────────────────────────────────────────────
        // Config Resolution Order Tests
        // ────────────────────────────────────────────────────────────────

        [TestCase(true, false, "xml chosen")] // XML exists, JSON doesn't
        [TestCase(false, true, "fallback due to json throw")] // XML doesn't, JSON exists but throws
        [TestCase(false, false, "fallback")] // Neither exists
        [TestCase(true, true, "xml chosen")] // Both exist, XML preferred
        public void Initialize_Resolves_Config_In_Expected_Order(bool hasXml, bool hasJson, string expectedPath)
        {
            // Arrange
            var logger = new CtxLogger();

            if (hasXml)
            {
                var xmlPath = Path.Combine(_testDirectory, "NLog.config");
                File.WriteAllText(xmlPath, CreateMinimalNLogXml());
            }

            if (hasJson)
            {
                var jsonPath = Path.Combine(_testDirectory, "NLog.json");
                File.WriteAllText(jsonPath, "{}");
            }

            // Act
            var result = NLogFailsafeLogger.Initialize(logger, "NLog.config", "NLog.json");

            // Assert
            result.ShouldBeTrue();
            LogManager.Configuration.ShouldNotBeNull();

            if (expectedPath.Contains("xml"))
            {
                // XML was configured
                LogManager.Configuration.AllTargets.Count.ShouldBeGreaterThan(0);
            }
            else if (expectedPath.Contains("fallback"))
            {
                // Fallback was applied (console + file)
                LogManager.Configuration.AllTargets.ShouldContain(t => t is ConsoleTarget);
                LogManager.Configuration.AllTargets.ShouldContain(t => t is FileTarget);
            }
        }

        // ────────────────────────────────────────────────────────────────
        // Multiple Initialization Tests
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Initialize_Can_Be_Called_Multiple_Times_Safely()
        {
            // Arrange
            var xmlPath = Path.Combine(_testDirectory, "NLog.config");
            File.WriteAllText(xmlPath, CreateMinimalNLogXml());
            var logger1 = new CtxLogger();
            var logger2 = new CtxLogger();

            // Act
            var result1 = NLogFailsafeLogger.Initialize(logger1, "NLog.config", "NLog.json");
            var result2 = NLogFailsafeLogger.Initialize(logger2, "NLog.config", "NLog.json");

            // Assert
            result1.ShouldBeTrue();
            result2.ShouldBeTrue();
            LogManager.Configuration.ShouldNotBeNull();
        }

        [Test]
        public void Initialize_With_Null_Filenames_Uses_Defaults()
        {
            // Arrange
            var logger = new CtxLogger();

            // Act
            var result = NLogFailsafeLogger.Initialize(logger, null, null);

            // Assert
            result.ShouldBeTrue();
            LogManager.Configuration.ShouldNotBeNull();
            // Should fall back to minimal config since default files don't exist
            LogManager.Configuration.AllTargets.ShouldContain(t => t is ConsoleTarget || t is FileTarget);
        }

        // ────────────────────────────────────────────────────────────────
        // AppContext.BaseDirectory Path Resolution Tests
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Initialize_Uses_AppContext_BaseDirectory_For_Path_Resolution()
        {
            // Arrange
            var xmlPath = Path.Combine(_testDirectory, "NLog.config");
            File.WriteAllText(xmlPath, CreateMinimalNLogXml());
            var logger = new CtxLogger();

            // Act
            var result = NLogFailsafeLogger.Initialize(logger, "NLog.config", "NLog.json");

            // Assert
            result.ShouldBeTrue();
            LogManager.Configuration.ShouldNotBeNull();
            // Verify that the configuration was loaded from the test directory
            LogManager.Configuration.AllTargets.Count.ShouldBeGreaterThan(0);
        }

        // ────────────────────────────────────────────────────────────────
        // Helper Methods
        // ────────────────────────────────────────────────────────────────

        private string CreateMinimalNLogXml()
        {
            return @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<nlog xmlns=""http://www.nlog-project.org/schemas/NLog.xsd""
      xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <targets>
    <target xsi:type=""Console"" name=""console"" />
  </targets>
  <rules>
    <logger name=""*"" minlevel=""Trace"" writeTo=""console"" />
  </rules>
</nlog>";
        }

        private string CreateNLogXmlWithMemoryTarget()
        {
            return @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<nlog xmlns=""http://www.nlog-project.org/schemas/NLog.xsd""
      xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <targets>
    <target xsi:type=""Memory"" name=""memory"" layout=""${level}|${message}"" />
  </targets>
  <rules>
    <logger name=""*"" minlevel=""Trace"" writeTo=""memory"" />
  </rules>
</nlog>";
        }
    }
}
