// tests/SeriLogShared.Tests/CtxLogger_LogLevel_Tests.cs

using LogCtxShared;
using SeriLogShared;
using NUnit.Framework.Internal;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Shouldly;
using System.Collections.Concurrent;

namespace SeriLogShared.Tests
{
    [TestFixture]
    [Category("unit")]
    [Parallelizable(ParallelScope.None)]
    public class CtxLogger_LogLevel_Tests
    {
        private TestSink _sink;
        // private Serilog.Core.Logger _testLogger;
        private CtxLogger _ctxLogger = new();

        [SetUp]
        public void SetUp()
        {
            _sink = new TestSink();
        }

        [TearDown]
        public void TearDown()
        {
            _ctxLogger?.Dispose();
            Log.CloseAndFlush();

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
