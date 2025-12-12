using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using LogCtxShared;

namespace LogCtxShared
{
    /// <summary>
    /// NLog context extensions for structured logging with caller information.
    /// Replaces LogCtx.Set() pattern with ILogger.SetContext() using BeginScope.
    /// </summary>
    public static class NLogContextExtensions
    {
        /// <summary>
        /// Sets logging context with automatic caller information capture.
        /// Returns IDisposable scope - use with 'using' statement to clear context.
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="props">Optional properties dictionary. If null, creates new Props.</param>
        /// <param name="memberName">Auto-captured caller method name</param>
        /// <param name="sourceFilePath">Auto-captured source file path</param>
        /// <param name="sourceLineNumber">Auto-captured source line number</param>
        /// <returns>IDisposable scope that clears context when disposed</returns>
        /// <example>
        /// using (_logger.SetContext(new Props().Add("UserId", userId)))
        /// {
        ///     _logger.LogInformation("User action completed");
        /// }
        /// </example>
        public static IDisposable SetContext(
            this ILogger logger,
            Props? props = null,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            props ??= new Props();

            // Build source context
            var fileName = System.IO.Path.GetFileNameWithoutExtension(sourceFilePath);
            var strace = SourceContext.BuildStackTrace(fileName, memberName, sourceLineNumber);

            // Add stack trace to props if not already present
            if (!props.ContainsKey(LogContextKeys.STRACE))
            {
                props.Add(LogContextKeys.STRACE, strace);
            }

            // BeginScope with the props dictionary - NLog captures all key-value pairs
            return logger.BeginScope(props) ?? new NullScope();
        }

        /// <summary>
        /// Sets operation-scoped context with named operation and properties.
        /// Convenience method for common pattern of tracking operations.
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="operationName">Name of the operation (e.g., "ProcessOrder")</param>
        /// <param name="properties">Additional properties as tuples</param>
        /// <returns>IDisposable scope that clears context when disposed</returns>
        /// <example>
        /// using (_logger.SetOperationContext("ProcessOrder", ("OrderId", 123), ("CustomerId", 456)))
        /// {
        ///     _logger.LogInformation("Processing order");
        /// }
        /// </example>
        public static IDisposable SetOperationContext(
            this ILogger logger,
            string operationName,
            params (string key, object value)[] properties)
        {
            var props = new Props { ["Operation"] = operationName };
            foreach (var (key, value) in properties)
            {
                props.Add(key, value);
            }
            return logger.SetContext(props);
        }

        // Null scope for cases where BeginScope returns null
        private class NullScope : IDisposable
        {
            public void Dispose() { }
        }
    }
}