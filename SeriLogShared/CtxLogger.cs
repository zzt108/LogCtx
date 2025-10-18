using LogCtxShared;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Context;
using System;
using System.IO;

namespace SeriLogShared
{
    public class CtxLogger : ILogCtxLogger
    {
        private static IConfigurationRoot? _configuration = null;
        private static bool _isConfigured = false;

        public CtxLogger()
        {
            // ✅ NEW: Initialize failsafe before trying to read configuration
            var baseDir = AppContext.BaseDirectory;
            FailsafeLogger.Initialize(baseDir);

            if (_configuration is not null)
            {
                Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(_configuration)
                    .CreateLogger();
                _isConfigured = true;
            }
        }

        public LogCtx Ctx { get => new LogCtx(new SeriLogScopeContext()); set => throw new NotImplementedException(); }

        public bool ConfigureJson(string configPath)
        {
            if (_isConfigured)
            {
                return true; // Already configured
            }

            try
            {
                _configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile(configPath)
                    .Build();
                Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(_configuration)
                    .CreateLogger();
                _isConfigured = true;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to configure Serilog from JSON: {ex.Message}");
                return false;
            }
        }

        public bool ConfigureXml(string configPath)
        {
            if (_isConfigured)
            {
                return true; // Already configured
            }

            try
            {
                _configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddXmlFile(configPath)
                    .Build();

                Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(_configuration)
                    .CreateLogger();
                _isConfigured = true;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to configure Serilog from XML: {ex.Message}");
                return false;
            }
        }

        public void Debug(string message)
        {
            Log.Debug(message);
        }

        public void Dispose()
        {
            Log.CloseAndFlush();
        }

        public void Error(Exception ex, string message)
        {
            Log.Error(ex, message);
        }

        public void Fatal(Exception ex, string message)
        {
            Log.Fatal(ex, message);
        }

        public void Info(string message)
        {
            Log.Information(message);
        }

        public void Trace(string message)
        {
            Log.Verbose(message);
        }

        public void Warn(string message)
        {
            Log.Warning(message);
        }
    }
}
