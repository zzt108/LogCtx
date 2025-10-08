// ✅ FULL FILE VERSION
// File: LogCtxShared/LogCtx.cs
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.IO;

namespace LogCtxShared
{
    /// <summary>
    /// Logger-agnostic contextual logging utility supporting both traditional and fluent APIs.
    /// Maintains full backward compatibility with existing AddProperty patterns.
    /// </summary>
    public sealed class LogCtx : IDisposable
    {
        private readonly Dictionary<string, object> _properties;
        private readonly string _memberName;
        private readonly string _sourceFilePath;
        private readonly int _sourceLineNumber;
        private bool _disposed;

        #region Static Logger Access (EXISTING PATTERN)
        
        /// <summary>
        /// ✅ EXISTING - Static logger access (set by LogCtx implementation)
        /// This is null until FailsafeLogger.Initialize() is called
        /// </summary>
        public static ILogCtxLogger? Logger { get; internal set; }

        /// <summary>
        /// ✅ EXISTING - Check if LogCtx can log
        /// </summary>
        public static bool CanLog => Logger != null;

        #endregion

        #region Existing Constructor & Methods (BACKWARD COMPATIBLE)
        
        /// <summary>
        /// ✅ EXISTING - Create new logging context with automatic caller info
        /// </summary>
        public static LogCtx Set([CallerMemberName] string memberName = "",
                               [CallerFilePath] string sourceFilePath = "",
                               [CallerLineNumber] int sourceLineNumber = 0)
        {
            return new LogCtx(memberName, sourceFilePath, sourceLineNumber);
        }

        private LogCtx(string memberName, string sourceFilePath, int sourceLineNumber)
        {
            _properties = new Dictionary<string, object>();
            _memberName = memberName;
            _sourceFilePath = sourceFilePath;
            _sourceLineNumber = sourceLineNumber;
            
            // ✅ EXISTING - Auto-add caller info
            AddProperty("MemberName", memberName);
            AddProperty("SourceFile", Path.GetFileName(sourceFilePath));
            AddProperty("LineNumber", sourceLineNumber);
        }

        /// <summary>
        /// ✅ EXISTING - Add property to context (traditional API)
        /// </summary>
        public void AddProperty(string key, object value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Property key cannot be null or empty", nameof(key));
                
            _properties[key] = value;
        }

        /// <summary>
        /// ✅ EXISTING - Get all properties
        /// </summary>
        public IReadOnlyDictionary<string, object> Properties => _properties;

        #endregion

        #region New Fluent API Extensions (ADDITIVE)

        /// <summary>
        /// 🆕 FLUENT - Add property and return this for chaining
        /// </summary>
        public LogCtx With(string key, object value)
        {
            AddProperty(key, value);
            return this;
        }

        /// <summary>
        /// 🆕 FLUENT - Add multiple properties from anonymous object
        /// </summary>
        public LogCtx With(object properties)
        {
            if (properties == null) return this;

            var type = properties.GetType();
            foreach (var prop in type.GetProperties())
            {
                AddProperty(prop.Name, prop.GetValue(properties) ?? "null");
            }
            return this;
        }

        /// <summary>
        /// 🆕 FLUENT - Add properties from dictionary
        /// </summary>
        public LogCtx With(IDictionary<string, object> properties)
        {
            if (properties == null) return this;
            
            foreach (var kvp in properties)
            {
                AddProperty(kvp.Key, kvp.Value);
            }
            return this;
        }

        /// <summary>
        /// 🆕 FLUENT - Conditional property addition
        /// </summary>
        public LogCtx WithIf(bool condition, string key, object value)
        {
            if (condition)
                AddProperty(key, value);
            return this;
        }

        /// <summary>
        /// 🆕 FLUENT - Add timing information
        /// </summary>
        public LogCtx WithTiming(string operationName, TimeSpan duration)
        {
            return With($"{operationName}DurationMs", duration.TotalMilliseconds)
                  .With($"{operationName}Duration", duration.ToString());
        }

        /// <summary>
        /// 🆕 FLUENT - Add exception context
        /// </summary>
        public LogCtx WithException(Exception ex, string prefix = "Error")
        {
            if (ex == null) return this;
            
            return With($"{prefix}Type", ex.GetType().Name)
                  .With($"{prefix}Message", ex.Message)
                  .With($"{prefix}StackTrace", ex.StackTrace ?? "");
        }

        #endregion

        #region Fluent Logging Methods (Null-Safe)

        /// <summary>
        /// 🆕 FLUENT - Log information and return context for further chaining
        /// Returns this context even if Logger is null (graceful degradation)
        /// </summary>
        public LogCtx LogInfo(string message)
        {
            Logger?.Info(message); // Uses existing ILogCtxLogger.Info
            return this;
        }

        /// <summary>
        /// 🆕 FLUENT - Log warning and return context
        /// </summary>
        public LogCtx LogWarning(string message)
        {
            Logger?.Warn(message); // Uses existing ILogCtxLogger.Warn
            return this;
        }

        /// <summary>
        /// 🆕 FLUENT - Log error and return context
        /// </summary>
        public LogCtx LogError(string message, Exception? ex = null)
        {
            if (ex != null)
                Logger?.Error(ex, message);
            else
                Logger?.Error(new InvalidOperationException(message), message);
            return this;
        }

        /// <summary>
        /// 🆕 FLUENT - Log debug and return context
        /// </summary>
        public LogCtx LogDebug(string message)
        {
            Logger?.Debug(message); // Uses existing ILogCtxLogger.Debug
            return this;
        }

        /// <summary>
        /// 🆕 FLUENT - Log fatal error and return context
        /// </summary>
        public LogCtx LogFatal(string message, Exception? ex = null)
        {
            if (ex != null)
                Logger?.Fatal(ex, message);
            else
                Logger?.Fatal(new InvalidOperationException(message), message);
            return this;
        }

        #endregion

        #region Existing Disposal (UNCHANGED)

        public void Dispose()
        {
            if (!_disposed)
            {
                // ✅ EXISTING - Cleanup logic unchanged
                _properties.Clear();
                _disposed = true;
            }
        }

        #endregion
    }
}
