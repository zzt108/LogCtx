namespace LogCtxShared
{
    /// <summary>
    /// Standard context property keys for structured logging.
    /// These keys appear in log sinks (e.g., SEQ) as queryable properties.
    /// Migrated from LogCtx.cs constants.
    /// </summary>
    public static class LogContextKeys
    {
        /// <summary>Source file name</summary>
        public const string FILE = "CTX_FILE";

        /// <summary>Source line number</summary>
        public const string LINE = "CTX_LINE";

        /// <summary>Method name</summary>
        public const string METHOD = "CTX_METHOD";

        /// <summary>Compact source location (File.Method.Line)</summary>
        public const string SRC = "CTX_SRC";

        /// <summary>Filtered stack trace with caller information</summary>
        public const string STRACE = "CTX_STRACE";
    }
}