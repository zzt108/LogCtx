using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace LogCtxShared
{
    /// <summary>
    /// NLog context extensions for structured logging with caller information.
    /// Replaces logger.SetContext pattern with fluent Props API.
    /// </summary>
    public static class NLogContextExtensions
    {
        /// <summary>
        /// Creates a new logging context scope with automatic caller information capture.
        /// Returns Props (IDisposable) - use with using statement.
        /// </summary>
        /// <example>
        /// <code>
        /// using Props p = _logger.SetContext()
        ///     .Add("userId", 123)
        ///     .Add("action", "login");
        /// {
        ///     _logger.LogInformation("User logged in");
        /// }
        /// </code>
        /// </example>
        public static Props SetContext(
            this ILogger logger,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            var fileName = System.IO.Path.GetFileNameWithoutExtension(sourceFilePath);
            return new Props(logger, null, fileName, memberName, sourceLineNumber);
        }

        /// <summary>
        /// Creates a nested logging context that builds upon an existing Props scope.
        /// The parent scope is disposed after properties are copied.
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="parent">Parent Props to inherit properties from</param>
        /// <param name="memberName">Auto-captured caller method name</param>
        /// <param name="sourceFilePath">Auto-captured source file path</param>
        /// <param name="sourceLineNumber">Auto-captured source line number</param>
        /// <returns>New Props with inherited properties and updated CTXSTRACE</returns>
        /// <example>
        /// <code>
        /// using Props p = _logger.SetContext()
        ///     .Add("userId", 123);
        /// {
        ///     _logger.LogInformation("Outer scope");
        ///     
        ///     p = _logger.SetContext(p)
        ///         .Add("action", "login");
        ///     _logger.LogInformation("Nested scope - has userId + action");
        /// }
        /// </code>
        /// </example>
        public static Props SetContext(
            this ILogger logger,
            Props parent,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            var fileName = System.IO.Path.GetFileNameWithoutExtension(sourceFilePath);

            // ✅ SAFETY: Copy parent properties BEFORE disposing
            // (Prevents race where parent could be mutated between dispose and copy)
            var newProps = new Props(logger, parent, fileName, memberName, sourceLineNumber);

            // ✅ Now safe to dispose parent (properties already copied)
            parent?.Dispose();

            return newProps;
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
        /// <code>
        /// using (_logger.SetOperationContext("ProcessOrder", ("OrderId", 123), ("CustomerId", 456)))
        /// {
        ///     _logger.LogInformation("Processing order");
        /// }
        /// </code>
        /// </example>
        public static IDisposable SetOperationContext(
            this ILogger logger,
            string operationName,
            params (string key, object value)[] properties)
        {
            var props = new Props(
                logger,
                null,
                "", // No specific file for operation context
                operationName,
                0);

            props["Operation"] = operationName;

            foreach (var (key, value) in properties)
            {
                props[key] = value;
            }

            return props;
        }
    }
}
