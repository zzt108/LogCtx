using Serilog;
using System;

namespace SeriLogShared
{
    // ✅ NEW — Serilog-specific failsafe logger initialization
    // Intent: Ensures at least a minimal Console + File logger exists, even when config files are missing.
    // Uses SelfLog for diagnosing Serilog configuration errors during tests/debug.
    internal static class SeriLogFailsafeLogger
    {
        public static void Initialize(string baseDir, string? jsonFile = "Config/LogConfig.json", string? xmlFile = "Config/LogConfig.xml")
        {
            try
            {
                // If Log.Logger is already configured (not a default empty logger), skip initialization
                if (Log.Logger != Serilog.Core.Logger.None)
                {
                    return;
                }

                // Enable SelfLog during DEBUG builds for diagnosing configuration issues
#if DEBUG
                Serilog.Debugging.SelfLog.Enable(Console.Error);
#endif

                // Apply minimal fallback: Console + File with Verbose level
                ApplyMinimalFallback(baseDir);
            }
            catch
            {
                // Absolutely never throw; if even fallback fails, set a no-op logger.
                ApplyNoOpFallback();
            }
        }

        private static void ApplyMinimalFallback(string baseDir)
        {
            // Create logs directory next to the app if possible.
            string logs = Path.Combine(baseDir, "logs");
            try { Directory.CreateDirectory(logs); } catch { /* ignore */ }

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.ffff}|{Level:u3}|{SourceContext}|{Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    Path.Combine(logs, "app.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 5,
                    fileSizeLimitBytes: 5_000_000,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.ffff}|{Level:u3}|{SourceContext}|{Message:lj}{NewLine}{Exception}")
                .CreateLogger();
        }

        private static void ApplyNoOpFallback()
        {
            // Minimal config that discards logs, ensuring no startup failure.
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Fatal()
                .CreateLogger();
        }
    }
}
