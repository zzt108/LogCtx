using LogCtxNameSpace;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Context;
using Serilog.Events;

namespace SeriLogAdapter
{

    public class SeriLogCtx:ILogCtxLogger
    {
        public IScopeContext ScopeContext { get=>new SeriLogScopeContext(); }
        IScopeContext ILogCtxLogger.ScopeContext { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool Configure(string configPath)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(configPath)
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();
            return true;
        }

        public void Debug(string message)
        {
            Log.Debug(message);
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
    }

    public class SeriLogScopeContext : IScopeContext
    {
        public void Clear()
        {
            LogContext.Reset();
        }

        public void PushProperty(string key, object value)
        {
            LogContext.PushProperty(key, value);
        }
    }

}
