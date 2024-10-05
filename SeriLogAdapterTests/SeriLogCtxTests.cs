using FluentAssertions;
using NUnit.Framework;
using SeriLogAdapter;

namespace SeriLogAdapter.Tests
{
    [TestFixture]
    public class SeriLogCtxTests
    {
        private const string ConfigPathJson = "Config/LogConfig.json";
        private const string ConfigPathXml = "Config/LogConfig.xml"; 

        [SetUp]
        public void Setup()
        {
            // Setup any required environment configuration for tests, e.g., setting up a logger
            //Log.Logger = new LoggerConfiguration()
            //    .MinimumLevel.Debug()
            //    .WriteTo.Console()
            //    .CreateLogger();
        }

        [Test]
        public void Configure_ShouldReadConfigurationFile()
        {
            // Arrange
            var seriLogCtx = new SeriLogCtx();

            // Act
            var result = seriLogCtx.ConfigureJson(ConfigPathJson);

            // Assert
            result.Should().BeTrue(); 
        }

        [Test]
        public void CanDoStructuredLog()
        {
            Serilog.Debugging.SelfLog.Enable(msg => Console.Error.WriteLine(msg));
            // Arrange
            using var log = new SeriLogCtx();
            var result = log.ConfigureXml(ConfigPathXml);

            // Act
            log.Ctx.Set(new Props("first", result, log));
            log.Debug("Debug");
            log.Fatal(new ArgumentException("Test Fatal Argument Exception", "Param name"), "Fatal");
            log.Error(new ArgumentException("Test Argument Exception", "Param name"), "Error");

            // Assert
            // Log.CloseAndFlush();
        }

        // Additional tests can be written to cover more functionality as needed.
    }
}