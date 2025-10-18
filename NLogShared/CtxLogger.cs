using LogCtxShared;
using NLog;
using NLog.Config;

namespace NLogShared
{
    public class CtxLogger : ILogCtxLogger
    {
        private static string? _logConfigPath = null;
        private static bool _isConfigured = false;

        private Logger? Logger;
        private LogCtxShared.LogCtx _ctx;

//        public LogCtx Ctx { get => new LogCtx(new NLogScopeContext()); set => throw new NotImplementedException(); }

        public LogCtxShared.LogCtx Ctx { get; }

        // public LogCtxShared.LogCtx Ctx
        // {
        //     get => _ctx;
        //     set => _ctx = value;
        // }

        public bool ConfigureJson(string configPath)
        {
            throw new NotImplementedException("Only XML configuration is supported");
        }

        public bool ConfigureXml(string? configPath)
        {
            //if (configPath is null)
            //{
            //    return false;
            //}

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

        public CtxLogger()
        {
            ConfigureXml(_logConfigPath);
            Logger = LogManager.GetCurrentClassLogger();
            Ctx = new LogCtxShared.LogCtx(new NLogScopeContext()); // Initialize the context
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