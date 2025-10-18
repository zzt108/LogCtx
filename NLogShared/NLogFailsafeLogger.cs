using NLog;
using NLog.Config;
using NLog.Targets;

namespace NLogShared
{
    // Intent: a single, robust entry point that never throws on logger startup.
    // It uses AppContext.BaseDirectory for stable pathing and falls back to a minimal in-memory config.
    internal static class NLogFailsafeLogger
    {
        public static bool Initialize(CtxLogger ctx, string? preferredFileName = "NLog.config", string? altJsonFileName = "NLog.json")
        {
            try
            {
                // 1) Stable base directory across VS, VS Code, and direct EXE launch.
                var baseDir = AppContext.BaseDirectory;
                var xmlPath = Path.Combine(baseDir, preferredFileName ?? "NLog.config");
                var jsonPath = Path.Combine(baseDir, altJsonFileName ?? "NLog.json");

                // 2) Try XML via existing LogCtx CtxLogger first.
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