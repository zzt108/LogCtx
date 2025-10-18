using LogCtxShared;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace NLogShared
{
    public class CtxLogger : ILogCtxLogger
    {
        private static string? _logConfigPath = null;
        private static bool _isConfigured = false;

        private Logger? Logger;
        private LogCtxShared.LogCtx _ctx;

        public LogCtxShared.LogCtx Ctx { get; }

        public CtxLogger(): this(_logConfigPath)
        {
        }

        public CtxLogger(string logConfigPath)
        {
            // ConfigureXml(logConfigPath);
            FailsafeLogger.Initialize(logConfigPath);
            Logger = LogManager.GetCurrentClassLogger();
            Ctx = new LogCtxShared.LogCtx(new NLogScopeContext()); // Initialize the context
        }

        public bool ConfigureJson(string configPath)
        {
            throw new NotImplementedException("Only XML configuration is supported");
        }

        public bool ConfigureXml(string? configPath)
        {

            if (_isConfigured)
            {
                return true; // Already configured
            }

            try
            {
                var config = new LoggingConfiguration();
                if (configPath is null) { configPath = "Config\\LogConfig.xml"; }
                // Use the modern way to configure
                LogManager.Setup().LoadConfigurationFromFile(configPath, optional: false);
                LogManager.AutoShutdown = true; // Ensure NLog cleans up on app exit
                _isConfigured = true;
                _logConfigPath = configPath;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.WriteLine("Failed to configure logger from XML.", configPath);
                return false;
                // throw new ArgumentException("Failed to configure logger from XML.", configPath);
            }
        }

        public void Debug(string message)
        {
            Logger?.Debug(message);
        }

        public void Dispose()
        {
            /*
            Dispose Method and LogManager.Shutdown():
Issue: Calling LogManager.Shutdown() in the Dispose method of each CtxLogger instance is incorrect. LogManager.Shutdown() shuts down the entire NLog system for the application. This should typically happen only once when the application exits. Disposing one CtxLogger instance shouldn't stop logging for other parts of the application.
Refactoring: Remove the LogManager.Shutdown() call from Dispose. NLog can often manage its shutdown automatically (especially with LogManager.AutoShutdown = true;). If manual shutdown is needed, do it at the application's exit point (e.g., in Program.cs or an application exit event handler). The Dispose method in CtxLogger might not even be necessary unless CtxLogger itself holds disposable resources (which it currently doesn't seem to, besides potentially the context if LogCtx were disposable). If ILogCtxLogger requires IDisposable, provide an empty implementation if CtxLogger has nothing to dispose.
            */

            LogManager.Flush(); // This will flush NLog
        }

        public void Error(Exception ex, string message)
        {
            Logger?.Error(ex, message);
        }

        public void Fatal(Exception ex, string message)
        {
            Logger?.Fatal(ex, message);
        }

        public void Info(string message)
        {
            Logger?.Info(message);
        }

        public void Trace(string message)
        {
            Logger?.Trace(message);
        }

        public void Warn(string message)
        {
            Logger?.Warn(message);
        }
    }

    public class NLogScopeContext : IScopeContext
    {
        public void Clear()
        {
            NLog.ScopeContext.Clear();
        }

        public void PushProperty(string key, object value)
        {
            NLog.ScopeContext.PushProperty(key, value);
        }
    }

        // Intent: a single, robust entry point that never throws on logger startup.
    // It uses AppContext.BaseDirectory for stable pathing and falls back to a minimal in-memory config.
    internal static class FailsafeLogger
    {
        public static bool Initialize(string? preferredFileName = "NLog.config", string? altJsonFileName = "NLog.json")
        {
            try
            {
                // 1) Stable base directory across VS, VS Code, and direct EXE launch.
                var baseDir = AppContext.BaseDirectory;
                var xmlPath = Path.Combine(baseDir, preferredFileName ?? "NLog.config");
                var jsonPath = Path.Combine(baseDir, altJsonFileName ?? "NLog.json");

                // 2) Try XML via existing LogCtx CtxLogger first.
                var ctx = new CtxLogger();
                if (File.Exists(xmlPath))
                {
                    var ok = ctx.ConfigureXml(xmlPath);
                    if (ok)
                        return true;
                }

                // 3) Try JSON as a second chance.
                if (File.Exists(jsonPath))
                {
                    var ok = ctx.ConfigureJson(jsonPath);
                    if (ok)
                        return true;
                }

                // 4) Last resort: build a minimal in-memory NLog config (console + rolling file).
                ApplyMinimalFallback(baseDir);
                return true;
            }
            catch
            {
                // Absolutely never throw; if even fallback fails, force a no-op logger.
                ApplyNoOpFallback();
                return true;
            }
        }

        private static void ApplyMinimalFallback(string baseDir)
        {
            // Create logs directory next to the app if possible.
            string logs = Path.Combine(baseDir, "logs");
            try { Directory.CreateDirectory(logs); } catch { /* ignore */ }

            var config = new LoggingConfiguration();

            var console = new ConsoleTarget("console")
            {
                Layout = "${longdate}|${level:uppercase=true}|${logger}|${message} ${exception:format=tostring}"
            };

            var file = new FileTarget("file")
            {
                FileName = Path.Combine(logs, "app.log"),
                ArchiveFileName = Path.Combine(logs, "app.{#}.log"),
                ArchiveAboveSize = 5_000_000,
                MaxArchiveFiles = 5,
                Layout = "${longdate}|${level:uppercase=true}|${logger}|${message} ${exception:format=tostring}"
            };

            config.AddTarget(console);
            config.AddTarget(file);
            config.AddRuleForAllLevels(console);
            config.AddRuleForAllLevels(file);

            LogManager.Configuration = config;
        }

        private static void ApplyNoOpFallback()
        {
            // Minimal config that discards logs, ensuring no startup failure.
            var config = new LoggingConfiguration();
            var nullTarget = new NullTarget("null");
            config.AddTarget(nullTarget);
            config.AddRuleForAllLevels(nullTarget);
            LogManager.Configuration = config;
        }
    }

}