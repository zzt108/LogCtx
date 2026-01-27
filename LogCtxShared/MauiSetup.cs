using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Targets;

namespace LogCtxShared;

/// <summary>
/// Static helper methods for adding NLog as the logging provider to an existing <see cref="ILoggingBuilder"/>.
/// can be used by:
/// - MAUI app: call it from MauiProgram using builder.Logging + builder.Configuration
/// - Tests/console apps: call it using LoggerFactory.Create(...) or DI host builder​
/// Now your MAUI app will wire it like this (in your MAUI project, not in tests):
/// - builder.Logging.AddNLogLogging(builder.Configuration);
/// </summary>
public static class MauiSetup
{
    /// <summary>
    /// Adds NLog as the logging provider to an existing <see cref="ILoggingBuilder"/>.
    /// This method is MAUI-independent (safe for unit test projects).
    /// </summary>
    public static ILoggingBuilder AddNLogLogging(
        this ILoggingBuilder logging,
        IConfiguration? configuration = null,
        string? seqUrl = null,
        string? apiKey = null,
        string nlogConfigFileName = "NLog.config",
        string? nlogConfigContent = null)
    {
        var resolvedSeqUrl = seqUrl ?? GetConfigValue(configuration, "Seq:Url", "seqUrl");
        var resolvedApiKey = apiKey ?? GetConfigValue(configuration, "Seq:ApiKey", "seqApiKey");

        SafeLoadNLogConfiguration(nlogConfigFileName, nlogConfigContent, resolvedSeqUrl, resolvedApiKey);
        ApplySeqVariables(resolvedSeqUrl, resolvedApiKey);

        logging.ClearProviders();
        logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);

        logging.AddNLog(new NLogProviderOptions
        {
            IncludeScopes = true,
            CaptureMessageTemplates = true,
            CaptureMessageProperties = true
        });

        return logging;
    }

    /// <summary>
    /// Convenience method for non-host scenarios (tests/console) that want an <see cref="ILoggerFactory"/>.
    /// </summary>
    public static ILoggerFactory CreateLoggerFactory(
        IConfiguration? configuration = null,
        string? seqUrl = null,
        string? apiKey = null,
        string nlogConfigFileName = "NLog.config")
    {
        return LoggerFactory.Create(builder =>
        {
            builder.AddNLogLogging(configuration, seqUrl, apiKey, nlogConfigFileName);
        });
    }

    private static void SafeLoadNLogConfiguration(string nlogConfigFileName, string? nlogConfigContent, string? seqUrl, string? apiKey)
    {
        try
        {
            // Enable internal logging to console/debug for troubleshooting
            NLog.Common.InternalLogger.LogToConsole = true;
            NLog.Common.InternalLogger.LogLevel = NLog.LogLevel.Trace;

            if (!string.IsNullOrWhiteSpace(nlogConfigContent))
            {
                System.Diagnostics.Debug.WriteLine("[LogCtx] Loading NLog config from XML string (with manual variable replacement).");

                // Manually replace variables to ensure they are available before targets initialize
                var processedXml = nlogConfigContent;
                if (!string.IsNullOrWhiteSpace(seqUrl))
                    processedXml = processedXml.Replace("${var:seqUrl}", seqUrl);
                if (!string.IsNullOrWhiteSpace(apiKey))
                    processedXml = processedXml.Replace("${var:seqApiKey}", apiKey);

                LogManager.Setup().LoadConfigurationFromXml(processedXml);
                return;
            }

            var configPath = ResolveConfigPath(nlogConfigFileName);
            System.Diagnostics.Debug.WriteLine($"[LogCtx] Attempting to load NLog config from file: {configPath}");

            if (File.Exists(configPath))
            {
                LogManager.Setup().LoadConfigurationFromFile(configPath);
                System.Diagnostics.Debug.WriteLine("[LogCtx] NLog config loaded from file.");
                return;
            }

            System.Diagnostics.Debug.WriteLine("[LogCtx] NLog config file not found, using fallback.");
            ApplyFallbackConsoleConfiguration();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LogCtx] Error in SafeLoadNLogConfiguration: {ex}");
            ApplyFallbackConsoleConfiguration();
        }
    }

    private static void ApplyFallbackConsoleConfiguration()
    {
        try
        {
            var config = new LoggingConfiguration();

            var console = new ConsoleTarget("console");
            config.AddTarget(console);
            config.AddRuleForAllLevels(console);

            LogManager.Configuration = config;
        }
        catch
        {
            // Absolute last resort: ignore.
        }
    }

    private static void ApplySeqVariables(string? seqUrl, string? apiKey)
    {
        try
        {
            var config = LogManager.Configuration;
            if (config == null)
            {
                System.Diagnostics.Debug.WriteLine("[LogCtx] ApplySeqVariables: LogManager.Configuration is NULL.");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[LogCtx] ApplySeqVariables: seqUrl='{seqUrl}', apiKey='{apiKey}'");

            if (!string.IsNullOrWhiteSpace(seqUrl))
            {
                config.Variables["seqUrl"] = seqUrl;
            }

            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                config.Variables["seqApiKey"] = apiKey;
            }

            LogManager.ReconfigExistingLoggers();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LogCtx] Error in ApplySeqVariables: {ex}");
        }
    }

    private static string ResolveConfigPath(string nlogConfigFileName)
    {
        var baseDir = AppContext.BaseDirectory ?? string.Empty;
        return Path.Combine(baseDir, nlogConfigFileName);
    }

    private static string? GetConfigValue(IConfiguration? configuration, params string[] keys)
    {
        if (configuration == null)
        {
            return null;
        }

        foreach (var key in keys)
        {
            var value = configuration[key];
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}