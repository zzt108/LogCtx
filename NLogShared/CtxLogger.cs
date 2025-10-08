// ✅ FULL FILE VERSION
// File: NLogShared/CtxLogger.cs

using System;
using System.Collections.Generic;
using NLog;
using NLog.Config;
using LogCtxShared;

namespace NLogShared
{
    /// <summary>
    /// NLog-specific implementation of ILogCtxLogger.
    /// Provides context-aware logging with structured properties support.
    /// Enhanced to support fluent LogCtx API patterns.
    /// </summary>
    public class CtxLogger : ILogCtxLogger
    {
        private static bool isConfigured = false;
        private readonly Logger _logger;
        private LogCtx _ctx = default!;

        #region Construction & Configuration

        public CtxLogger()
        {
            // Delay logger creation until configuration
            _logger = LogManager.GetCurrentClassLogger();
        }

        public bool ConfigureJson(string configPath)
        {
            // JSON config not supported in this implementation
            return false;
        }

        public bool ConfigureXml(string configPath)
        {
            if (string.IsNullOrEmpty(configPath)) return false;
            if (isConfigured) return true;

            var config = new XmlLoggingConfiguration(configPath);
            LogManager.Configuration = config;
            isConfigured = true;
            return true;
        }

        #endregion

        #region Context Management

        /// <summary>
        /// ✅ EXISTING - Current logging context
        /// </summary>
        public LogCtx Ctx
        {
            get => _ctx;
            set => _ctx = value;
        }

        #endregion

        #region Traditional Logging Methods

        public void Debug(string message)      => _logger.Debug(message);
        public void Info(string message)       => _logger.Info(message);
        public void Warn(string message)       => _logger.Warn(message);
        public void Error(Exception ex, string message) => _logger.Error(ex, message);
        public void Fatal(Exception ex, string message) => _logger.Fatal(ex, message);
        public void Trace(string message)      => _logger.Trace(message);

        #endregion

        #region Enhanced Context Logging Methods

        public void Debug(string message, LogCtx context = null)
            => LogWithContext(() => _logger.Debug(message), context);
        
        public void Info(string message, LogCtx context = null)
            => LogWithContext(() => _logger.Info(message), context);

        public void Warn(string message, LogCtx context = null)
            => LogWithContext(() => _logger.Warn(message), context);

        public void Error(Exception ex, string message, LogCtx context = null)
            => LogWithContext(() => _logger.Error(ex, message), context);

        public void Fatal(Exception ex, string message, LogCtx context = null)
            => LogWithContext(() => _logger.Fatal(ex, message), context);

        public void Trace(string message, LogCtx context = null)
            => LogWithContext(() => _logger.Trace(message), context);

        #endregion

        #region Context Application Helper

        private void LogWithContext(Action logAction, LogCtx? context)
        {
            if (context == null || context.Properties.Count == 0)
            {
                logAction();
                return;
            }

            var disposables = new List<IDisposable>();
            try
            {
                foreach (var kvp in context.Properties)
                {
                    disposables.Add(NLog.ScopeContext.PushProperty(kvp.Key, kvp.Value));
                }
                logAction();
            }
            finally
            {
                foreach (var d in disposables)
                {
                    try { d.Dispose(); }
                    catch { /* swallow */ }
                }
            }
        }

        #endregion

        #region Disposal

        public void Dispose()
        {
            try
            {
                LogManager.Flush();
            }
            catch
            {
                // swallow
            }
        }

        #endregion
    }
}
