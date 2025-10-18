using NUnit.Framework;
using LogCtxShared;
using SeriLogShared;
using Shouldly;
using Serilog;
using Serilog.Events;
using System.Collections.Generic;
using System.Linq;

namespace SeriLogAdapter.Tests
{
    [TestFixture]
    public class SeriLogCtxTests
    {
        private const string ConfigPathJson = "Config/LogConfig.json";
        private const string ConfigPathXml = "Config/LogConfig.xml";

        // ✅ NEW: In-memory sink for capturing log events during tests
        private InMemorySink _sink;

        [SetUp]
        public void Setup()
        {
            // ✅ NEW: Setup in-memory sink for test verification
            // Alternative: Use Serilog.Sinks.TestCorrelator for more sophisticated test scenarios
            _sink = new InMemorySink();

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Sink(_sink)
                .CreateLogger();
        }

        [TearDown]
        public void TearDown()
        {
            Log.CloseAndFlush();
        }

        [Test]
        public void Configure_ShouldReadConfigurationFile()
        {
            // Arrange
            var seriLogCtx = new CtxLogger();

            // Act
            var result = seriLogCtx.ConfigureJson(ConfigPathJson);

            // Assert
            result.ShouldBeTrue();
        }

        [Test]
        public void CanDoStructuredLog()
        {
            Serilog.Debugging.SelfLog.Enable(msg => Console.Error.WriteLine(msg));
            // Arrange
            using var log = new CtxLogger();
            var result = log.ConfigureXml(ConfigPathXml);

            // Act
            log.Ctx.Set(new Props("first", result, log));
            log.Debug("Debug");
            log.Fatal(new ArgumentException("Test Fatal Argument Exception", "Param name"), "Fatal");
            log.Error(new ArgumentException("Test Argument Exception", "Param name"), "Error");

            // Assert
            // Log.CloseAndFlush();
        }

        // ✅ NEW: Test Debug level writes to in-memory sink
        [Test]
        public void Debug_Writes_Debug_Level()
        {
            // Arrange
            using var log = new CtxLogger();
            _sink.Events.Clear();

            // Act
            log.Debug("debug message");
            Log.CloseAndFlush();

            // Assert
            var evt = _sink.Events.FirstOrDefault(e => e.Level == LogEventLevel.Debug);
            evt.ShouldNotBeNull();
            evt.RenderMessage().ShouldBe("debug message");
        }

        [Test]
        public void Info_Writes_Information_Level()
        {
            // Arrange
            using var log = new CtxLogger();
            _sink.Events.Clear();

            // Act
            log.Info("info message");
            Log.CloseAndFlush();

            // Assert
            var evt = _sink.Events.FirstOrDefault(e => e.Level == LogEventLevel.Information);
            evt.ShouldNotBeNull();
            evt.RenderMessage().ShouldBe("info message");
        }

        // ✅ NEW: Test Warning level writes to in-memory sink
        [Test]
        public void Warn_Writes_Warning_Level()
        {
            // Arrange
            using var log = new CtxLogger();
            _sink.Events.Clear();

            // Act
            log.Warn("warning message");
            Log.CloseAndFlush();

            // Assert
            var evt = _sink.Events.FirstOrDefault(e => e.Level == LogEventLevel.Warning);
            evt.ShouldNotBeNull();
            evt.RenderMessage().ShouldBe("warning message");
        }

        // ✅ NEW: Test Error level writes to in-memory sink
        [Test]
        public void Error_Writes_Error_Level()
        {
            // Arrange
            using var log = new CtxLogger();
            _sink.Events.Clear();
            var exception = new InvalidOperationException("test error");

            // Act
            log.Error(exception, "error message");
            Log.CloseAndFlush();

            // Assert
            var evt = _sink.Events.FirstOrDefault(e => e.Level == LogEventLevel.Error);
            evt.ShouldNotBeNull();
            evt.RenderMessage().ShouldBe("error message");
            evt.Exception.ShouldBe(exception);
        }

        // ✅ NEW: Test Fatal level writes to in-memory sink
        [Test]
        public void Fatal_Writes_Fatal_Level()
        {
            // Arrange
            using var log = new CtxLogger();
            _sink.Events.Clear();
            var exception = new Exception("fatal error");

            // Act
            log.Fatal(exception, "fatal message");
            Log.CloseAndFlush();

            // Assert
            var evt = _sink.Events.FirstOrDefault(e => e.Level == LogEventLevel.Fatal);
            evt.ShouldNotBeNull();
            evt.RenderMessage().ShouldBe("fatal message");
            evt.Exception.ShouldBe(exception);
        }

        // ✅ NEW: Test Trace (Verbose) level writes to in-memory sink
        [Test]
        public void Trace_Writes_Verbose_Level()
        {
            // Arrange
            using var log = new CtxLogger();
            _sink.Events.Clear();

            // Act
            log.Trace("trace message");
            Log.CloseAndFlush();

            // Assert
            var evt = _sink.Events.FirstOrDefault(e => e.Level == LogEventLevel.Verbose);
            evt.ShouldNotBeNull();
            evt.RenderMessage().ShouldBe("trace message");
        }

        // ✅ NEW: Simple in-memory sink for capturing Serilog events
        // Alternative: Use Serilog.Sinks.TestCorrelator NuGet package for production-grade testing
        private class InMemorySink : Serilog.Core.ILogEventSink
        {
            public List<LogEvent> Events { get; } = new List<LogEvent>();

            public void Emit(LogEvent logEvent)
            {
                Events.Add(logEvent);
            }
        }
    }
}
