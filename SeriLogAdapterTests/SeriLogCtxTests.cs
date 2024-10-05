using FluentAssertions;
using NUnit.Framework;
using SeriLogAdapter;
using LogCtxNameSpace;

namespace SeriLogAdapter.Tests
{
    [TestFixture]
    public class SeriLogCtxTests
    {
        private const string ConfigPath = "Config/LogConfig.json"; // Set the appropriate path for the config

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
            var result = seriLogCtx.Configure(ConfigPath);

            // Assert
            result.Should().BeTrue(); 
        }

        [Test]
        public void ScopeContext_ShouldNotBeNullAfterInitialization()
        {
            // Arrange
            var seriLogCtx = new SeriLogCtx();

            // Act
            var scopeContext = seriLogCtx.ScopeContext;

            // Assert
            scopeContext.Should().NotBeNull();
            scopeContext.Should().BeOfType<SeriLogScopeContext>();
        }

        [Test]
        public void CanDoStructuredLog()
        {
            // Arrange
            var log = new SeriLogCtx();
            var result = log.Configure(ConfigPath);
            LogCtx.Init(log.ScopeContext);


            // Act
            using var p = LogCtx.Set();
            log.Debug("Debug");
            log.Fatal(null, "Fatal");

            // Assert
        }

        // Additional tests can be written to cover more functionality as needed.
    }
}