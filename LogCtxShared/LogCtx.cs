// ✅ FULL FILE VERSION
// File: LogCtxShared/LogCtx.cs

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.IO;

namespace LogCtxShared
{
    /// <summary>
    /// Contextual logging utility supporting both traditional and fluent APIs.
    /// Maintains full backward compatibility with existing AddProperty/Logger patterns.
    /// </summary>
    public sealed class LogCtx : IDisposable
    {
        private readonly Dictionary<string, object> _properties;
        private readonly string _memberName;
        private readonly string _sourceFilePath;
        private readonly int _sourceLineNumber;
        private bool _disposed;

        #region Static Logger Access

        /// <summary>
        /// Static logger access (set by FailsafeLogger.Initialize)
        /// </summary>
        public static ILogCtxLogger? Logger { get; internal set; }

        /// <summary>
        /// Indicates if a logger has been configured
        /// </summary>
        public static bool CanLog => Logger != null;

        #endregion

        #region Constructors & Factories

        /// <summary>
        /// ✅ EXISTING - Create new logging context with automatic caller info
        /// </summary>
        public static LogCtx Set(
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            return new LogCtx(memberName, sourceFilePath, sourceLineNumber);
        }

        /// <summary>
        /// 🆕 NEW OVERLOAD - Create context from Props and add all properties
        /// </summary>
        public static LogCtx Set(
            Props props,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            var ctx = new LogCtx(memberName, sourceFilePath, sourceLineNumber);
            ctx.With(props.Properties);
            return ctx;
        }

        private LogCtx(string memberName, string sourceFilePath, int sourceLineNumber)
        {
            _properties = new Dictionary<string, object>();
            _memberName = memberName;
            _sourceFilePath = sourceFilePath;
            _sourceLineNumber = sourceLineNumber;

            // Auto-add caller info
            AddProperty("MemberName", memberName);
            AddProperty("SourceFile", Path.GetFileName(sourceFilePath));
            AddProperty("LineNumber", sourceLineNumber);
        }

        #endregion

        #region Traditional API

        /// <summary>
        /// ✅ EXISTING - Add property to context
        /// </summary>
        public void AddProperty(string key, object value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Property key cannot be null or empty", nameof(key));
            _properties[key] = value;
        }

        /// <summary>
        /// ✅ EXISTING - Read-only view of properties
        /// </summary>
        public IReadOnlyDictionary<string, object> Properties => _properties;

        #endregion

        #region Fluent Extensions

        public LogCtx With(string key, object value)
        {
            AddProperty(key, value);
            return this;
        }

        public LogCtx With(object properties)
        {
            if (properties == null) return this;
            foreach (var prop in properties.GetType().GetProperties())
                AddProperty(prop.Name, prop.GetValue(properties) ?? "null");
            return this;
        }

        public LogCtx With(IDictionary<string, object> properties)
        {
            if (properties == null) return this;
            foreach (var kv in properties)
                AddProperty(kv.Key, kv.Value);
            return this;
        }

        public LogCtx WithIf(bool condition, string key, object value)
        {
            if (condition) AddProperty(key, value);
            return this;
        }

        public LogCtx WithTiming(string operationName, TimeSpan duration)
            => With($"{operationName}DurationMs", duration.TotalMilliseconds)
               .With($"{operationName}Duration", duration.ToString());

        public LogCtx WithException(Exception ex, string prefix = "Error")
        {
            if (ex == null) return this;
            return With($"{prefix}Type", ex.GetType().Name)
                   .With($"{prefix}Message", ex.Message)
                   .With($"{prefix}StackTrace", ex.StackTrace ?? "");
        }

        #endregion

        #region Fluent Logging

        public LogCtx LogInfo(string message)
        {
            Logger?.Info(message, this);
            return this;
        }

        public LogCtx LogWarning(string message)
        {
            Logger?.Warn(message, this);
            return this;
        }

        public LogCtx LogError(string message, Exception? ex = null)
        {
            if (ex != null)
                Logger?.Error(ex, message, this);
            else
                Logger?.Error(new InvalidOperationException(message), message, this);
            return this;
        }

        public LogCtx LogDebug(string message)
        {
            Logger?.Debug(message, this);
            return this;
        }

        public LogCtx LogFatal(string message, Exception? ex = null)
        {
            if (ex != null)
                Logger?.Fatal(ex, message, this);
            else
                Logger?.Fatal(new InvalidOperationException(message), message, this);
            return this;
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            if (!_disposed)
            {
                _properties.Clear();
                _disposed = true;
            }
        }

        #endregion
    }
}
