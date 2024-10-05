using FluentAssertions;
using NUnit.Framework;
using SeriLogAdapter;
using LogCtxNameSpace;
using Serilog;

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
            Serilog.Debugging.SelfLog.Enable(msg => Console.Error.WriteLine(msg));
            // Arrange
            var log = new SeriLogCtx();
            var result = log.Configure(ConfigPath);

            //var outputTemplate = $"[{{Timestamp:HH:mm:ss,fff}} {{Level:u3}}] {{Message:lj}} [{{{LogCtx.FILE}}}.{{{LogCtx.METHOD}}}]{{NewLine}}{{Exception}}";
            ////var outputTemplate = $"[{{Timestamp:mm:ss,fff}} {{Level:u3}}] {{Message:lj}}{{NewLine}}{{Exception}}";
            //Log.Logger = new Serilog.LoggerConfiguration()
            //    .MinimumLevel.Verbose()
            //    .Enrich.FromLogContext()
            //    .WriteTo.Console(Serilog.Events.LogEventLevel.Warning, outputTemplate: outputTemplate)
            //    .WriteTo.Seq("http://localhost:5341")
            //    .CreateLogger();

            LogCtx.Init(log.ScopeContext);


            // Act
            using var p = LogCtx.Set();
            log.Debug("Debug");
            log.Fatal(null, "Fatal");

            // Assert
            Log.CloseAndFlush();
        }

        // Additional tests can be written to cover more functionality as needed.
    }
}