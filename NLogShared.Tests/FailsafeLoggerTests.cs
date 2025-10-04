using NUnit.Framework;
using Shouldly;
using System;
using System.IO;
using NLog;
using NLogShared;

namespace NLogShared.Tests
{
    [TestFixture]
    public class FailsafeLoggerTests
    {
        private string baseDir = AppContext.BaseDirectory;

        [Test]
        public void Should_initialize_when_no_config_files()
        {
            // Arrange: ensure neither NLog.config nor NLog.json exists in base directory
            var xml = Path.Combine(baseDir, "NLog.config");
            var json = Path.Combine(baseDir, "NLog.json");
            if (File.Exists(xml)) File.Delete(xml);
            if (File.Exists(json)) File.Delete(json);

            // Act
            var ok = FailsafeLogger.Initialize();

            // Assert
            ok.ShouldBeTrue();
            LogManager.Configuration.ShouldNotBeNull();
        }

        [Test]
        public void Should_fallback_on_invalid_xml()
        {
            // Arrange: create an invalid XML
            var xml = Path.Combine(baseDir, "NLog.config");
            File.WriteAllText(xml, "<nlog><targets></nlog>"); // malformed

            // Act
            var ok = FailsafeLogger.Initialize();

            // Assert
            ok.ShouldBeTrue();
            LogManager.Configuration.ShouldNotBeNull();

            // Cleanup
            File.Delete(xml);
        }

        [Test]
        public void Should_use_valid_xml_when_present()
        {
            // Arrange: minimal valid NLog config
            var xml = Path.Combine(baseDir, "NLog.config");
            File.WriteAllText(xml,
@"<?xml version=""1.0"" encoding=""utf-8"" ?>
<nlog xmlns=""http://www.nlog-project.org/schemas/NLog.xsd""
      xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <targets>
    <target xsi:type=""Console"" name=""console"" layout=""${message}"" />
  </targets>
  <rules>
    <logger name=""*"" minlevel=""Info"" writeTo=""console"" />
  </rules>
</nlog>");

            // Act
            var ok = FailsafeLogger.Initialize();

            // Assert
            ok.ShouldBeTrue();
            LogManager.Configuration.ShouldNotBeNull();

            // Cleanup
            File.Delete(xml);
        }
    }
}
