// tests/SeriLogShared.Tests/CtxLogger_LogLevel_Tests.cs

using NLogShared;
using NUnit.Framework;
using NUnit.Framework.Internal;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using SeriLogShared;
using Shouldly;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace SeriLogShared.Tests
{
    [TestFixture]
    [Category("unit")]
    [Parallelizable(ParallelScope.None)]
    public class CtxLogger_LogLevel_Tests
    {
        private TestSink _sink;
        private Serilog.Core.Logger _testLogger;
        private CtxLogger _ctxLogger = new();

        // private CtxLogger Log = new();


        [SetUp]
        public void SetUp()
        {
            _sink = new TestSink();
            _testLogger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Sink(_sink)
                .CreateLogger();

            Log.CloseAndFlush();
            Log.Logger = _testLogger;

            _ctxLogger = new CtxLogger();
        }

        [TearDown]
        public void TearDown()
        {
            _testLogger?.Dispose();
            _ctxLogger?.Dispose();
        }

        [Test]
        public void Trace_Writes_Verbose_Level()
        {
            // Act
            _ctxLogger.Trace("trace message");

            // Assert
            var evt = _sink.Events.LastOrDefault();
            evt.ShouldNotBeNull();
            evt.Level.ShouldBe(LogEventLevel.Verbose);
            evt.RenderMessage().ShouldBe("trace message");
            evt.Exception.ShouldBeNull();
        }

        [Test]
        public void Debug_Writes_Debug_Level()
        {
            // Act
            _ctxLogger.Debug("debug message");

            // Assert
            var evt = _sink.Events.LastOrDefault();
            evt.ShouldNotBeNull();
            evt.Level.ShouldBe(LogEventLevel.Debug);
            evt.RenderMessage().ShouldBe("debug message");
            evt.Exception.ShouldBeNull();
        }

        [Test]
        public void Info_Writes_Information_Level()
        {
            // Act
            _ctxLogger.Info("info message");

            // Assert
            var evt = _sink.Events.LastOrDefault();
            evt.ShouldNotBeNull();
            evt.Level.ShouldBe(LogEventLevel.Information);
            evt.RenderMessage().ShouldBe("info message");
            evt.Exception.ShouldBeNull();
        }

        [Test]
        public void Warn_Writes_Warning_Level()
        {
            // Act
            _ctxLogger.Warn("warn message");

            // Assert
            var evt = _sink.Events.LastOrDefault();
            evt.ShouldNotBeNull();
            evt.Level.ShouldBe(LogEventLevel.Warning);
            evt.RenderMessage().ShouldBe("warn message");
            evt.Exception.ShouldBeNull();
        }

        [Test]
        public void Error_Writes_Error_Level_With_Exception()
        {
            // Arrange
            var ex = new InvalidOperationException("boom");

            // Act
            _ctxLogger.Error(ex, "error message");

            // Assert
            var evt = _sink.Events.LastOrDefault();
            evt.ShouldNotBeNull();
            evt.Level.ShouldBe(LogEventLevel.Error);
            evt.RenderMessage().ShouldBe("error message");
            evt.Exception.ShouldNotBeNull();
            evt.Exception.Message.ShouldContain("boom");
        }

        [Test]
        public void Fatal_Writes_Fatal_Level_With_Exception()
        {
            // Arrange
            var ex = new ApplicationException("fatality");

            // Act
            _ctxLogger.Fatal(ex, "fatal message");

            // Assert
            var evt = _sink.Events.LastOrDefault();
            evt.ShouldNotBeNull();
            evt.Level.ShouldBe(LogEventLevel.Fatal);
            evt.RenderMessage().ShouldBe("fatal message");
            evt.Exception.ShouldNotBeNull();
            evt.Exception.Message.ShouldContain("fatality");
        }

        private sealed class TestSink : ILogEventSink
        {
            private readonly ConcurrentQueue<LogEvent> _events = new ConcurrentQueue<LogEvent>();

            public void Emit(LogEvent logEvent)
            {
                _events.Enqueue(logEvent);
            }

            public LogEvent[] Events => _events.ToArray();
        }
    }
}
