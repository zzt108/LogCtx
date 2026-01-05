using System.Collections.Concurrent;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace LogCtxShared
{
    /// <summary>
    /// Thread-safe fluent properties dictionary for structured logging context.
    /// Compatible with ILogger.BeginScope - all properties automatically captured by NLog.
    /// </summary>
    /// <remarks>
    /// Usage:
    /// <code>
    /// using Props p = _logger.SetContext()
    ///     .Add("userId", 123)
    ///     .Add("action", "login");
    /// {
    ///     _logger.LogInformation("User logged in");
    ///
    ///     p = _logger.SetContext(p)
    ///         .Add("step", "validation");
    ///     _logger.LogInformation("Nested context");
    /// }
    /// </code>
    /// </remarks>
    public class Props : ConcurrentDictionary<string, object>, IDisposable
    {
        private readonly ILogger _logger;
        private readonly string _sourceFileName;
        private readonly string _memberName;
        private readonly int _lineNumber;
        private IDisposable? _scope;
        private int _disposed = 0;

        internal Props(
            ILogger logger,
            ConcurrentDictionary<string, object>? parentProps = null,
            string? sourceFileName = null,
            string? memberName = null,
            int? lineNumber = null)
        {
            _logger = logger;
            _sourceFileName = sourceFileName ?? "N/A";
            _memberName = memberName ?? "N/A";
            _lineNumber = lineNumber ?? 0;

            // ✅ Copy parent props (thread-safe snapshot)
            if (parentProps != null)
            {
                foreach (var kv in parentProps)
                {
                    TryAdd(kv.Key, kv.Value);
                }
            }

            // Add CTXSTRACE if not present (from parent or new)
            if (!ContainsKey(LogContextKeys.STRACE))
            {
                var strace = SourceContext.BuildStackTrace(_sourceFileName, _memberName, _lineNumber);
                this[LogContextKeys.STRACE] = strace;
            }

            // ✅ Create scope after initialization
            _scope = _logger.BeginScope(this);
        }

        /// <summary>
        /// Adds or updates a property and recreates the logging scope.
        /// Thread-safe operation.
        /// </summary>
        public Props Add(string key, object? value)
        {
            // ✅ Thread-safe upsert via indexer
            this[key] = value ?? "null value";

            // ⚠️ Recreate scope to ensure NLog captures updated properties
            // (Test NLogScopeReferenceTests determines if this is needed)
            RecreateScope();

            return this;
        }

        public Props Add(IDictionary<string, object> props)
        {
            foreach (var kv in props)
            {
                this[kv.Key] = kv.Value;
            }
            return this;
        }

        /// <summary>
        /// Adds property with JSON serialization.
        /// </summary>
        public Props AddJson(string key, object value, Formatting formatting = Formatting.None)
        {
            this[key] = JsonConvert.SerializeObject(value, formatting);
            RecreateScope();
            return this;
        }

        /// <summary>
        /// Clears all properties and recreates empty scope.
        /// </summary>
        public new Props Clear()
        {
            base.Clear();
            RecreateScope();
            return this;
        }

        /// <summary>
        /// Recreates the MEL scope with current properties.
        /// Thread-safe operation.
        /// </summary>
        private void RecreateScope()
        {
            if (_disposed == 1) return;

            var oldScope = Interlocked.Exchange(ref _scope, null);
            oldScope?.Dispose();

            _scope = _logger.BeginScope(this);
        }

        public void Dispose()
        {
            // ✅ Thread-safe dispose (once only via Interlocked)
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                var scopeToDispose = Interlocked.Exchange(ref _scope, null);
                scopeToDispose?.Dispose();
            }
        }
    }
}