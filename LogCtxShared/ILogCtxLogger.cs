// ✅ FULL FILE VERSION
// File: LogCtxShared/ILogCtxLogger.cs

using System;

namespace LogCtxShared
{
    /// <summary>
    /// Logger-agnostic interface for contextual logging.
    /// Implementations handle the specific logging framework integration.
    /// </summary>
    public interface ILogCtxLogger : IDisposable
    {
        #region Existing Methods (BACKWARD COMPATIBLE)

        /// <summary>
        /// ✅ EXISTING - Configure from XML file
        /// </summary>
        bool ConfigureXml(string configPath);

        /// <summary>
        /// ✅ EXISTING - Configure from JSON file
        /// </summary>
        bool ConfigureJson(string configPath);

        /// <summary>
        /// ✅ EXISTING - Log debug message
        /// </summary>
        void Debug(string message);

        /// <summary>
        /// ✅ EXISTING - Log information message
        /// </summary>
        void Info(string message);

        /// <summary>
        /// ✅ EXISTING - Log warning message
        /// </summary>
        void Warn(string message);

        /// <summary>
        /// ✅ EXISTING - Log error with exception
        /// </summary>
        void Error(Exception ex, string message);

        /// <summary>
        /// ✅ EXISTING - Log fatal error with exception
        /// </summary>
        void Fatal(Exception ex, string message);

        /// <summary>
        /// ✅ EXISTING - Log trace message
        /// </summary>
        void Trace(string message);

        #endregion

        #region Enhanced Context Support (NEW - ADDITIVE)

        /// <summary>
        /// 🆕 ENHANCED - Debug with context properties
        /// </summary>
        void Debug(string message, LogCtx context = null);

        /// <summary>
        /// 🆕 ENHANCED - Info with context properties
        /// </summary>
        void Info(string message, LogCtx context = null);

        /// <summary>
        /// 🆕 ENHANCED - Warning with context properties
        /// </summary>
        void Warn(string message, LogCtx context = null);

        /// <summary>
        /// 🆕 ENHANCED - Error with context properties
        /// </summary>
        void Error(Exception ex, string message, LogCtx context = null);

        /// <summary>
        /// 🆕 ENHANCED - Fatal with context properties
        /// </summary>
        void Fatal(Exception ex, string message, LogCtx context = null);

        /// <summary>
        /// 🆕 ENHANCED - Trace with context properties
        /// </summary>
        void Trace(string message, LogCtx context = null);

        #endregion

        #region Context Management (EXISTING)

        /// <summary>
        /// ✅ EXISTING - Current logging context
        /// </summary>
        LogCtx Ctx { get; set; }

        #endregion
    }
}
