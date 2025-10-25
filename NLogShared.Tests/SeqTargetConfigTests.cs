// ✅ FULL FILE VERSION
// NLogShared.Tests/SeqTargetConfigTests.cs
// Project: NLogShared.Tests
// Purpose: Unit tests validating Seq target presence in NLog configuration without requiring a live SEQ server

using NUnit.Framework;
using Shouldly;
using NLogShared;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Seq;
using System;
using System.IO;
using System.Linq;

namespace NLogShared.Tests
{
    [TestFixture]
    [Category("unit")]
    public class SeqTargetConfigTests
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
        // Seq Target Presence Tests
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Seq_Target_Configured_When_Present_In_Config()
        {
            // Arrange
            var xmlPath = Path.Combine(_testDirectory, "NLog.config");
            File.WriteAllText(xmlPath, CreateNLogXmlWithSeqTarget());
            var logger = new CtxLogger();

            // Act
            var result = logger.ConfigureXml(xmlPath);

            // Assert
            result.ShouldBeTrue();
            LogManager.Configuration.ShouldNotBeNull();

            var seqTarget = LogManager.Configuration.AllTargets
                .OfType<SeqTarget>()
                .FirstOrDefault();

            seqTarget.ShouldNotBeNull();
            seqTarget.Name.ShouldBe("seq");
        }

        [Test]
        public void Seq_Target_Has_ServerUrl_Property()
        {
            // Arrange
            var xmlPath = Path.Combine(_testDirectory, "NLog.config");
            File.WriteAllText(xmlPath, CreateNLogXmlWithSeqTarget());
            var logger = new CtxLogger();

            // Act
            logger.ConfigureXml(xmlPath);

            // Assert
            var seqTarget = LogManager.Configuration.AllTargets
                .OfType<SeqTarget>()
                .FirstOrDefault();

            seqTarget.ShouldNotBeNull();
            seqTarget.ServerUrl.ShouldBe("http://localhost:5341");
        }

        [Test]
        public void Seq_Target_Is_Included_In_AllTargets_Collection()
        {
            // Arrange
            var xmlPath = Path.Combine(_testDirectory, "NLog.config");
            File.WriteAllText(xmlPath, CreateNLogXmlWithSeqTarget());
            var logger = new CtxLogger();

            // Act
            logger.ConfigureXml(xmlPath);

            // Assert
            LogManager.Configuration.AllTargets.Count.ShouldBeGreaterThanOrEqualTo(1);
            LogManager.Configuration.AllTargets.Any(t => t is SeqTarget).ShouldBeTrue();
        }

        [Test]
        public void Seq_Target_Can_Be_Found_By_Name()
        {
            // Arrange
            var xmlPath = Path.Combine(_testDirectory, "NLog.config");
            File.WriteAllText(xmlPath, CreateNLogXmlWithSeqTarget());
            var logger = new CtxLogger();

            // Act
            logger.ConfigureXml(xmlPath);

            // Assert
            var seqTarget = LogManager.Configuration.FindTargetByName<SeqTarget>("seq");
            seqTarget.ShouldNotBeNull();
            seqTarget.Name.ShouldBe("seq");
        }

        // ────────────────────────────────────────────────────────────────
        // Seq Target Configuration Properties Tests
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Seq_Target_ApiKey_Can_Be_Configured()
        {
            // Arrange
            var xmlPath = Path.Combine(_testDirectory, "NLog.config");
            File.WriteAllText(xmlPath, CreateNLogXmlWithSeqTargetAndApiKey());
            var logger = new CtxLogger();

            // Act
            logger.ConfigureXml(xmlPath);

            // Assert
            var seqTarget = LogManager.Configuration.FindTargetByName<SeqTarget>("seq");
            seqTarget.ShouldNotBeNull();
            seqTarget.ApiKey.ShouldBe("test-api-key-12345");
        }

        [Test]
        public void Seq_Target_Properties_Can_Be_Configured()
        {
            // Arrange
            var xmlPath = Path.Combine(_testDirectory, "NLog.config");
            File.WriteAllText(xmlPath, CreateNLogXmlWithSeqTargetAndProperties());
            var logger = new CtxLogger();

            // Act
            logger.ConfigureXml(xmlPath);

            // Assert
            var seqTarget = LogManager.Configuration.FindTargetByName<SeqTarget>("seq");
            seqTarget.ShouldNotBeNull();
            seqTarget.Properties.Count.ShouldBeGreaterThan(0);
        }

        // ────────────────────────────────────────────────────────────────
        // Multiple Targets Tests
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Config_With_Multiple_Targets_Includes_Seq()
        {
            // Arrange
            var xmlPath = Path.Combine(_testDirectory, "NLog.config");
            File.WriteAllText(xmlPath, CreateNLogXmlWithMultipleTargets());
            var logger = new CtxLogger(xmlPath);

            // Act
            logger.ConfigureXml(xmlPath);

            // Assert
            LogManager.Configuration.AllTargets.Count.ShouldBeGreaterThanOrEqualTo(3);
            LogManager.Configuration.AllTargets.Any(t => t is SeqTarget).ShouldBeTrue();
            LogManager.Configuration.AllTargets.Any(t => t is ConsoleTarget).ShouldBeTrue();
            LogManager.Configuration.AllTargets.Any(t => t is FileTarget).ShouldBeTrue();
        }

        [Test]
        public void Seq_Target_Can_Coexist_With_Other_Targets()
        {
            // Arrange
            var xmlPath = Path.Combine(_testDirectory, "NLog.config");
            File.WriteAllText(xmlPath, CreateNLogXmlWithMultipleTargets());
            var logger = new CtxLogger();

            // Act
            logger.ConfigureXml(xmlPath);

            // Assert
            var seqTarget = LogManager.Configuration.FindTargetByName<SeqTarget>("seq");
            var consoleTarget = LogManager.Configuration.FindTargetByName<ConsoleTarget>("console");
            var fileTarget = LogManager.Configuration.FindTargetByName<FileTarget>("file");

            seqTarget.ShouldNotBeNull();
            consoleTarget.ShouldNotBeNull();
            fileTarget.ShouldNotBeNull();
        }

        // ────────────────────────────────────────────────────────────────
        // Logging Rules Tests
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Seq_Target_Is_Included_In_Logging_Rules()
        {
            // Arrange
            var xmlPath = Path.Combine(_testDirectory, "NLog.config");
            File.WriteAllText(xmlPath, CreateNLogXmlWithSeqTarget());
            var logger = new CtxLogger();

            // Act
            logger.ConfigureXml(xmlPath);

            // Assert
            LogManager.Configuration.LoggingRules.ShouldNotBeEmpty();
            var rulesWithSeq = LogManager.Configuration.LoggingRules
                .Where(r => r.Targets.Any(t => t is SeqTarget))
                .ToList();
            rulesWithSeq.Count.ShouldBeGreaterThan(0);
        }

        [Test]
        public void Seq_Target_Receives_All_Log_Levels()
        {
            // Arrange
            var xmlPath = Path.Combine(_testDirectory, "NLog.config");
            File.WriteAllText(xmlPath, CreateNLogXmlWithSeqTarget());
            var logger = new CtxLogger();

            // Act
            logger.ConfigureXml(xmlPath);

            // Assert
            var rule = LogManager.Configuration.LoggingRules
                .First(r => r.Targets.Any(t => t is SeqTarget));

            rule.Levels.ShouldContain(LogLevel.Trace);
            rule.Levels.ShouldContain(LogLevel.Debug);
            rule.Levels.ShouldContain(LogLevel.Info);
            rule.Levels.ShouldContain(LogLevel.Warn);
            rule.Levels.ShouldContain(LogLevel.Error);
            rule.Levels.ShouldContain(LogLevel.Fatal);
        }

        // ────────────────────────────────────────────────────────────────
        // NLog.Targets.Seq Package Tests
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void SeqTarget_Type_Is_Available_From_NLog_Targets_Seq_Package()
        {
            // Arrange & Act
            var seqType = typeof(SeqTarget);

            // Assert
            seqType.ShouldNotBeNull();
            seqType.FullName.ShouldBe("NLog.Targets.Seq.SeqTarget");
        }

        [Test]
        public void SeqTarget_Inherits_From_TargetWithLayout()
        {
            // Arrange
            var seqTarget = new SeqTarget();

            // Assert
            seqTarget.ShouldBeAssignableTo<TargetWithLayout>();
        }

        // ────────────────────────────────────────────────────────────────
        // Config Without Seq Tests
        // ────────────────────────────────────────────────────────────────

        [Test]
        public void Config_Without_Seq_Does_Not_Include_SeqTarget()
        {
            // Arrange
            var xmlPath = Path.Combine(_testDirectory, "NLog.config");
            File.WriteAllText(xmlPath, CreateMinimalNLogXml());
            var logger = new CtxLogger();

            // Act
            logger.ConfigureXml(xmlPath);

            // Assert
            var seqTargets = LogManager.Configuration.AllTargets.OfType<SeqTarget>().ToList();
            seqTargets.Count.ShouldBe(0);
        }

        // ────────────────────────────────────────────────────────────────
        // Helper Methods
        // ────────────────────────────────────────────────────────────────

        private string CreateNLogXmlWithSeqTarget()
        {
            return @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<nlog xmlns=""http://www.nlog-project.org/schemas/NLog.xsd""
      xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <targets>
    <target xsi:type=""Seq"" name=""seq"" serverUrl=""http://localhost:5341"" />
  </targets>
  <rules>
    <logger name=""*"" minlevel=""Trace"" writeTo=""seq"" />
  </rules>
</nlog>";
        }

        private string CreateNLogXmlWithSeqTargetAndApiKey()
        {
            return @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<nlog xmlns=""http://www.nlog-project.org/schemas/NLog.xsd""
      xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <targets>
    <target xsi:type=""Seq"" name=""seq"" 
            serverUrl=""http://localhost:5341"" 
            apiKey=""test-api-key-12345"" />
  </targets>
  <rules>
    <logger name=""*"" minlevel=""Trace"" writeTo=""seq"" />
  </rules>
</nlog>";
        }

        private string CreateNLogXmlWithSeqTargetAndProperties()
        {
            return @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<nlog xmlns=""http://www.nlog-project.org/schemas/NLog.xsd""
      xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <targets>
    <target xsi:type=""Seq"" name=""seq"" serverUrl=""http://localhost:5341"">
      <property name=""Application"" value=""NLogShared.Tests"" />
      <property name=""Environment"" value=""Test"" />
    </target>
  </targets>
  <rules>
    <logger name=""*"" minlevel=""Trace"" writeTo=""seq"" />
  </rules>
</nlog>";
        }

        private string CreateNLogXmlWithMultipleTargets()
        {
            return @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<nlog xmlns=""http://www.nlog-project.org/schemas/NLog.xsd""
      xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <targets>
    <target xsi:type=""Console"" name=""console"" />
    <target xsi:type=""File"" name=""file"" fileName=""app.log"" />
    <target xsi:type=""Seq"" name=""seq"" serverUrl=""http://localhost:5341"" />
  </targets>
  <rules>
    <logger name=""*"" minlevel=""Trace"" writeTo=""console,file,seq"" />
  </rules>
</nlog>";
        }

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
    }
}
