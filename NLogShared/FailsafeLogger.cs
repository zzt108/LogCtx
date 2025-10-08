// ✅ FULL FILE VERSION
// File: NLogShared/FailsafeLogger.cs

using System;
using System.IO;
using NLog;
using NLog.Config;
using NLog.Targets;
using LogCtxShared;

namespace NLogShared
{
    /// <summary>
    /// Intent: a single, robust entry point that never throws on logger startup.
    /// It uses AppContext.BaseDirectory for stable pathing and falls back to a minimal in-memory config.
    /// 🆕 ENHANCED - Now properly initializes LogCtx.Logger for fluent API support.
    /// </summary>
    public static class FailsafeLogger
    {
        public static bool Initialize(string? preferredFileName = "NLog.config", string? altJsonFileName = "NLog.json")
        {
            try
            {
                var baseDir = AppContext.BaseDirectory;
                var xmlPath = Path.Combine(baseDir, preferredFileName ?? "NLog.config");
                var jsonPath = Path.Combine(baseDir, altJsonFileName ?? "NLog.json");

                var ctx = new CtxLogger();

                if (File.Exists(xmlPath) && ctx.ConfigureXml(xmlPath))
                {
                    LogCtx.Logger = ctx;
                    return true;
                }

                if (File.Exists(jsonPath) && ctx.ConfigureJson(jsonPath))
                {
                    LogCtx.Logger = ctx;
                    return true;
                }

                ApplyMinimalFallback(baseDir);

                ctx = new CtxLogger();
                ctx.ConfigureXml(null);
                LogCtx.Logger = ctx;
                
                return true;
            }
            catch
            {
                ApplyNoOpFallback();
                try
                {
                    var noOpCtx = new CtxLogger();
                    LogCtx.Logger = noOpCtx;
                }
                catch
                {
                    LogCtx.Logger = null!;
                }
                return true;
            }
        }

        private static void ApplyMinimalFallback(string baseDir)
        {
            string logs = Path.Combine(baseDir, "logs");
            try { Directory.CreateDirectory(logs); } catch { }

            var config = new LoggingConfiguration();

            var console = new ConsoleTarget("console")
            {
                Layout = "${longdate} ${level:uppercase=true} ${logger} ${message} ${exception:format=toString}"
            };

            var file = new FileTarget("file")
            {
                FileName = Path.Combine(logs, "app.log"),
                ArchiveFileName = Path.Combine(logs, "app.{#}.log"),
                ArchiveAboveSize = 5_000_000,
                MaxArchiveFiles = 5,
                Layout = "${longdate} ${level:uppercase=true} ${logger} ${message} ${exception:format=toString}"
            };

            config.AddTarget(console);
            config.AddTarget(file);
            config.AddRuleForAllLevels(console);
            config.AddRuleForAllLevels(file);

            LogManager.Configuration = config;
        }

        private static void ApplyNoOpFallback()
        {
            var config = new LoggingConfiguration();
            var nullTarget = new NullTarget("null");
            config.AddTarget(nullTarget);
            config.AddRuleForAllLevels(nullTarget);
            LogManager.Configuration = config;
        }
    }
}
