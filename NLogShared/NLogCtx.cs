using LogCtxShared;
using NLog;

namespace NLogAdapter
{
    public class NLogCtx :ILogCtxLogger
    {
        private Logger? Logger;

        public LogCtx Ctx { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool ConfigureJson(string configPath)
        {
            throw new NotImplementedException();
        }

        public bool ConfigureXml(string configPath)
        {
            throw new NotImplementedException();
        }

        public void Debug(string message)
        {
            Logger?.Debug(message);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
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

        public NLogCtx()
        {
            Logger = LogManager.GetCurrentClassLogger();
        }

        public void Trace(string message)
        {
            Logger?.Trace(message);
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
