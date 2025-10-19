// ✅ FULL FILE VERSION
// tests/SeriLogShared.Tests/Infra/TestSink.cs

using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace SeriLogShared.Tests.Infra
{
    /// <summary>
    /// Thread-safe in-memory sink for capturing Serilog events in tests.
    /// Single responsibility: collect and expose LogEvent data for assertions.
    /// </summary>
    public sealed class TestSink : ILogEventSink
    {
        private readonly ConcurrentQueue<LogEvent> _events = new ConcurrentQueue<LogEvent>();

        public void Emit(LogEvent logEvent)
        {
            if (logEvent == null) return;
            _events.Enqueue(logEvent);
        }

        public LogEvent[] Events => _events.ToArray();

        public int Count => _events.Count;

        public void Clear()
        {
            while (_events.TryDequeue(out _)) { }
        }

        public LogEvent? LastOrDefault() => _events.LastOrDefault();

        public LogEvent[] ByLevel(LogEventLevel level)
            => _events.Where(e => e.Level == level).ToArray();

        public LogEvent[] WithProperty(string key)
            => _events.Where(e => e.Properties.ContainsKey(key)).ToArray();

        public LogEvent[] WithPropertyEquals(string key, string expected)
            => _events.Where(e =>
                    e.Properties.TryGetValue(key, out var v)
                    && v is ScalarValue sv
                    && (sv.Value?.ToString() ?? string.Empty) == expected)
                .ToArray();

        public bool WaitForCount(int expected, TimeSpan? timeout = null)
        {
            var until = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(2));
            while (DateTime.UtcNow < until)
            {
                if (Count >= expected) return true;
                Thread.Sleep(10);
            }
            return Count >= expected;
        }
    }
}
