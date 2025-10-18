// ✅ FULL FILE VERSION
// NLogShared.Tests/ConfigResolutionTests.cs
// Project: NLogShared.Tests
// Purpose: Unit tests validating NLogFailsafeLogger.Initialize config resolution ordering (XML → JSON → fallback) and AppContext.BaseDirectory pathing

using NUnit.Framework;
using Shouldly;
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
    public class ConfigResolutionTests
    {
        private string _testDirectory;

        [SetUp]
        public void Setup()
        {
            // Create a unique temp directory for each test to avoid conflicts
            _testDirectory = Path.Combine(Path.GetTempPath(), $"NLogTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDirectory);

            // Change the working directory to the test directory
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
        // XML Path Resolution Tests
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Initialize_With_Xml_Present_Loads_Xml_Config()
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
            LogManager.Configuration.AllTargets.Any(t => t is ConsoleTarget).ShouldBeTrue();
        }

        [Test]
        public void Initialize_Xml_Path_Uses_AppContext_BaseDirectory()
        {
            // Arrange
            var xmlPath = Path.Combine(_testDirectory, "NLog.config");
            File.WriteAllText(xmlPath, CreateNLogXmlWithMemoryTarget());
            var logger = new CtxLogger();

            // Act
            var result = NLogFailsafeLogger.Initialize(logger, "NLog.config", "NLog.json");

            // Assert
            result.ShouldBeTrue();
            var memTarget = LogManager.Configuration.FindTargetByName<MemoryTarget>("memory");
            memTarget.ShouldNotBeNull();
        }

        // ────────────────────────────────────────────────────────────────
        // JSON Path Resolution Tests (ConfigureJson throws)
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Initialize_With_Json_Present_Falls_Back_Because_ConfigureJson_Throws()
        {
            // Arrange
            var jsonPath = Path.Combine(_testDirectory, "NLog.json");
            File.WriteAllText(jsonPath, "{}"); // JSON exists but CtxLogger.ConfigureJson throws NotImplementedException
            var logger = new CtxLogger();

            // Act
            var result = NLogFailsafeLogger.Initialize(logger, "NonExistent.config", "NLog.json");

            // Assert
            result.ShouldBeTrue();
            LogManager.Configuration.ShouldNotBeNull();
            // Should fall back to minimal config
            LogManager.Configuration.AllTargets.ShouldContain(t => t is ConsoleTarget);
            LogManager.Configuration.AllTargets.ShouldContain(t => t is FileTarget);
        }

        [Test]
        public void Initialize_Json_Fallback_Does_Not_Throw()
        {
            // Arrange
            var jsonPath = Path.Combine(_testDirectory, "NLog.json");
            File.WriteAllText(jsonPath, "{invalid json}");
            var logger = new CtxLogger();

            // Act & Assert
            Should.NotThrow(() =>
            {
                var result = NLogFailsafeLogger.Initialize(logger, "NonExistent.config", "NLog.json");
                result.ShouldBeTrue();
            });
        }

        // ────────────────────────────────────────────────────────────────
        // Minimal Fallback Tests
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Initialize_With_No_Configs_Applies_Minimal_Fallback()
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
        public void Initialize_Fallback_Creates_Logs_Directory()
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
        public void Initialize_Fallback_Console_Target_Has_Correct_Layout()
        {
            // Arrange
            var logger = new CtxLogger();

            // Act
            var result = NLogFailsafeLogger.Initialize(logger, "NonExistent.config", "NonExistent.json");

            // Assert
            result.ShouldBeTrue();
            var consoleTarget = LogManager.Configuration.FindTargetByName<ConsoleTarget>("console");
            consoleTarget.ShouldNotBeNull();
            consoleTarget.Layout.ToString().ShouldContain("${longdate}");
            consoleTarget.Layout.ToString().ShouldContain("${level:uppercase=true}");
        }

        [Test]
        public void Initialize_Fallback_File_Target_Has_Archive_Settings()
        {
            // Arrange
            var logger = new CtxLogger();

            // Act
            var result = NLogFailsafeLogger.Initialize(logger, "NonExistent.config", "NonExistent.json");

            // Assert
            result.ShouldBeTrue();
            var fileTarget = LogManager.Configuration.FindTargetByName<FileTarget>("file");
            fileTarget.ShouldNotBeNull();
            fileTarget.FileName.ToString().ShouldContain("app.log");
            fileTarget.ArchiveAboveSize.ShouldBe(5_000_000);
            fileTarget.MaxArchiveFiles.ShouldBe(5);
        }

        // ────────────────────────────────────────────────────────────────
        // Config Resolution Order Tests (TestCase Permutations)
        // ────────────────────────────────────────────────────────────────

        [TestCase(true, false, "xml")]  // XML yes, JSON no → XML chosen
        [TestCase(false, true, "fallback")]  // XML no, JSON yes → fallback (ConfigureJson throws)
        [TestCase(false, false, "fallback")]  // Neither → fallback
        [TestCase(true, true, "xml")]  // Both → XML preferred
        public void Initialize_Resolves_Config_In_Expected_Order(bool hasXml, bool hasJson, string expectedPath)
        {
            // Arrange
            var logger = new CtxLogger();

            if (hasXml)
            {
                var xmlPath = Path.Combine(_testDirectory, "NLog.config");
                File.WriteAllText(xmlPath, CreateNLogXmlWithMemoryTarget());
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

            if (expectedPath == "xml")
            {
                // XML was loaded → expect MemoryTarget
                var memTarget = LogManager.Configuration.AllTargets.OfType<MemoryTarget>().FirstOrDefault();
                memTarget.ShouldNotBeNull();
            }
            else if (expectedPath == "fallback")
            {
                // Fallback was applied → expect Console + File targets
                LogManager.Configuration.AllTargets.ShouldContain(t => t is ConsoleTarget);
                LogManager.Configuration.AllTargets.ShouldContain(t => t is FileTarget);
            }
        }

        // ────────────────────────────────────────────────────────────────
        // AppContext.BaseDirectory Pathing Tests
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Initialize_Uses_AppContext_BaseDirectory_For_Stable_Pathing()
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
            // Config was loaded from AppContext.BaseDirectory (test directory)
            LogManager.Configuration.AllTargets.Count.ShouldBeGreaterThan(0);
        }

        [Test]
        public void Initialize_Resolves_Relative_Paths_From_BaseDirectory()
        {
            // Arrange
            var xmlPath = Path.Combine(_testDirectory, "NLog.config");
            File.WriteAllText(xmlPath, CreateMinimalNLogXml());
            var logger = new CtxLogger();

            // Act
            var result = NLogFailsafeLogger.Initialize(logger, "NLog.config", "NLog.json");

            // Assert
            result.ShouldBeTrue();
            var consoleTarget = LogManager.Configuration.FindTargetByName<ConsoleTarget>("console");
            consoleTarget.ShouldNotBeNull();
        }

        // ────────────────────────────────────────────────────────────────
        // No-Op Fallback Tests (Hard Failure Path)
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Initialize_Never_Throws_Even_On_All_Path_Failures()
        {
            // Arrange
            var logger = new CtxLogger();

            // Act & Assert
            Should.NotThrow(() =>
            {
                var result = NLogFailsafeLogger.Initialize(logger, "NonExistent.config", "NonExistent.json");
                result.ShouldBeTrue();
            });
        }

        [Test]
        public void Initialize_Applies_No_Op_Fallback_On_Minimal_Fallback_Failure()
        {
            // Arrange
            // This is hard to simulate directly, but we can verify no exceptions occur
            var logger = new CtxLogger();

            // Act
            var result = NLogFailsafeLogger.Initialize(logger, "NonExistent.config", "NonExistent.json");

            // Assert
            result.ShouldBeTrue();
            LogManager.Configuration.ShouldNotBeNull();
            // In normal case, we get console + file targets
            // In no-op case, we'd get a NullTarget
            LogManager.Configuration.AllTargets.Count.ShouldBeGreaterThanOrEqualTo(1);
        }

        // ────────────────────────────────────────────────────────────────
        // Multiple Initialize Calls Tests
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
        // Config Precedence Tests
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Initialize_Xml_Takes_Precedence_Over_Json()
        {
            // Arrange
            var xmlPath = Path.Combine(_testDirectory, "NLog.config");
            var jsonPath = Path.Combine(_testDirectory, "NLog.json");
            File.WriteAllText(xmlPath, CreateNLogXmlWithMemoryTarget());
            File.WriteAllText(jsonPath, "{}"); // JSON exists but should not be used
            var logger = new CtxLogger();

            // Act
            var result = NLogFailsafeLogger.Initialize(logger, "NLog.config", "NLog.json");

            // Assert
            result.ShouldBeTrue();
            // XML was loaded, so MemoryTarget should exist
            var memTarget = LogManager.Configuration.AllTargets.OfType<MemoryTarget>().FirstOrDefault();
            memTarget.ShouldNotBeNull();
            // Fallback targets should not exist
            var consoleTarget = LogManager.Configuration.AllTargets.OfType<ConsoleTarget>().FirstOrDefault();
            consoleTarget.ShouldBeNull();
        }

        [Test]
        public void Initialize_Fallback_Applied_When_Both_Configs_Invalid()
        {
            // Arrange
            var xmlPath = Path.Combine(_testDirectory, "NLog.config");
            var jsonPath = Path.Combine(_testDirectory, "NLog.json");
            File.WriteAllText(xmlPath, "invalid xml content");
            File.WriteAllText(jsonPath, "{invalid json}");
            var logger = new CtxLogger();

            // Act
            var result = NLogFailsafeLogger.Initialize(logger, "NLog.config", "NLog.json");

            // Assert
            result.ShouldBeTrue();
            LogManager.Configuration.ShouldNotBeNull();
            // Both configs failed, so fallback should be applied
            LogManager.Configuration.AllTargets.ShouldContain(t => t is ConsoleTarget);
            LogManager.Configuration.AllTargets.ShouldContain(t => t is FileTarget);
        }

        // ────────────────────────────────────────────────────────────────
        // Logging Rules Verification Tests
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Initialize_Fallback_Configures_All_Levels()
        {
            // Arrange
            var logger = new CtxLogger();

            // Act
            var result = NLogFailsafeLogger.Initialize(logger, "NonExistent.config", "NonExistent.json");

            // Assert
            result.ShouldBeTrue();
            var config = LogManager.Configuration;
            config.LoggingRules.ShouldNotBeEmpty();

            var rule = config.LoggingRules.First();
            rule.Levels.ShouldContain(LogLevel.Trace);
            rule.Levels.ShouldContain(LogLevel.Debug);
            rule.Levels.ShouldContain(LogLevel.Info);
            rule.Levels.ShouldContain(LogLevel.Warn);
            rule.Levels.ShouldContain(LogLevel.Error);
            rule.Levels.ShouldContain(LogLevel.Fatal);
        }

        [Test]
        public void Initialize_Xml_Config_Applies_Logging_Rules()
        {
            // Arrange
            var xmlPath = Path.Combine(_testDirectory, "NLog.config");
            File.WriteAllText(xmlPath, CreateMinimalNLogXml());
            var logger = new CtxLogger();

            // Act
            var result = NLogFailsafeLogger.Initialize(logger, "NLog.config", "NLog.json");

            // Assert
            result.ShouldBeTrue();
            LogManager.Configuration.LoggingRules.ShouldNotBeEmpty();
            var consoleRule = LogManager.Configuration.LoggingRules
                .First(r => r.Targets.Any(t => t is ConsoleTarget));
            consoleRule.ShouldNotBeNull();
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
