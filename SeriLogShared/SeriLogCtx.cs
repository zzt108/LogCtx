using LogCtxShared;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Context;
using Serilog.Events;

namespace SeriLogAdapter
{

    public class SeriLogCtx:ILogCtxLogger
    {
        public LogCtx Ctx { get => new LogCtx(new SeriLogScopeContext()); set => throw new NotImplementedException(); }

        public bool ConfigureJson(string configPath)
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

        public bool ConfigureXml(string configPath)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddXmlFile(configPath)
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

    public class Props : LogCtxShared.Props
    {
        public Props(params object[] args):base(args) 
        {
        }

    }
}
