using LogCtxShared;
using NLog;
using NLog.Config;
using NLog.Targets;
using System;

namespace NLogShared
{
    public class CtxLogger : ILogCtxLogger
    {
        private static string? _logConfigPath = null;
        private Logger? Logger;
        private LogCtxShared.LogCtx _ctx;

//        public LogCtx Ctx { get => new LogCtx(new NLogScopeContext()); set => throw new NotImplementedException(); }

        public LogCtxShared.LogCtx Ctx
        {
            get => _ctx;
            set => _ctx = value;
        }

        public bool ConfigureJson(string configPath)
        {
            throw new NotImplementedException("Only XML configuration is supported");
        }

        public bool ConfigureXml(string? configPath)
        {
            if (configPath is null)
            {
                return false;
            }

            try
            {
                var config = new LoggingConfiguration();
                // TODO this is obsolete, should use LogManager.Setup
                LogManager.LoadConfiguration(configPath);
                _logConfigPath = configPath;
                return true;
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Failed to configure logger from XML.", configPath);
            }
        }

        public void Debug(string message)
        {
            Logger?.Debug(message);
        }

        public void Dispose()
        {
            // Dispose of any resources if necessary
            LogManager.Shutdown(); // This will flush and close down NLog
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

        public CtxLogger()
        {
            ConfigureXml(_logConfigPath);
            Logger = LogManager.GetCurrentClassLogger();
            _ctx = new LogCtxShared.LogCtx(new NLogScopeContext()); // Initialize the context
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
}