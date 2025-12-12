// ✅ FULL FILE VERSION
// tests/SeriLogShared.Tests/CtxLogger_Context_Tests.cs

using NUnit.Framework;
using Shouldly;
using SeriLogShared;
using LogCtxShared;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace SeriLogShared.Tests
{
    [TestFixture]
    [Category("unit")]
    [Parallelizable(ParallelScope.None)]
    public class CtxLogger_Context_Tests
    {
        private TestSink _sink;
        private CtxLogger _ctxLogger;

        [SetUp]
        public void SetUp()
        {
            _sink = new TestSink();

            _ctxLogger = new CtxLogger();
        }

        [TearDown]
        public void TearDown()
        {
            _ctxLogger?.Dispose();
            Log.CloseAndFlush();
        }

        [Test]
        public void Context_Props_Appear_In_LogEvent_Properties()
        {
            // Arrange
            var ctx = _ctxLogger.Ctx;
            var props = new Props("A", "B");

            // Act
            LogCtx.Set(props);
            _ctxLogger.Info("hello");

            // Assert
            var evt = _sink.Events.LastOrDefault();
            evt.ShouldNotBeNull();
            HasScalar(evt, "P00", "A").ShouldBeTrue("Expected P00=A in log event properties");
            HasScalar(evt, "P01", "B").ShouldBeTrue("Expected P01=B in log event properties");
        }

        [Test]
        public void Context_CtxStrace_Present_In_LogEvent()
        {
            // Arrange
            var ctx = _ctxLogger.Ctx;

            // Act
            LogCtx.Set(new Props("X"));
            _ctxLogger.Info("with strace");

            // Assert
            var evt = _sink.Events.LastOrDefault();
            evt.ShouldNotBeNull();
            evt.Properties.ContainsKey("CTX_STRACE").ShouldBeTrue();
            var strace = GetScalarString(evt, "CTX_STRACE");
            strace.ShouldNotBeNullOrWhiteSpace();
        }

        [Test]
        public void Context_Refresh_Replaces_Previous_Props()
        {
            // Arrange
            var ctx = _ctxLogger.Ctx;

            // Act 1
            LogCtx.Set(new Props("one"));
            _ctxLogger.Info("first");
            var first = _sink.Events.LastOrDefault();
            HasScalar(first, "P00", "one").ShouldBeTrue();

            // Act 2
            LogCtx.Set(new Props("two"));
            _ctxLogger.Info("second");
            var second = _sink.Events.LastOrDefault();

            // Assert
            HasScalar(second, "P00", "two").ShouldBeTrue();
        }

        private static bool HasScalar(LogEvent evt, string key, string expected)
            => evt.Properties.TryGetValue(key, out var v)
               && v is ScalarValue sv
               && (sv.Value?.ToString() ?? "") == expected;

        private static string GetScalarString(LogEvent evt, string key)
            => evt.Properties.TryGetValue(key, out var v) && v is ScalarValue sv
                ? sv.Value?.ToString()
                : null;

        private sealed class TestSink : ILogEventSink
        {
            private readonly ConcurrentQueue<LogEvent> _events = new ConcurrentQueue<LogEvent>();

            public void Emit(LogEvent logEvent) => _events.Enqueue(logEvent);

            public LogEvent[] Events => _events.ToArray();
        }
    }
}