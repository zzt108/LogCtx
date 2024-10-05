using AIA_Test.Common;
using FluentAssertions.Equivalency;
using Newtonsoft.Json;
using RNet;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using System;

namespace AIA_Test
{
    [TestFixture]
    public class MiscTest
    {
        [Test]
        public void CanCheckDecimaDigits()
        {
            //var field = "1.23";
            var field = "-1.2";

            // Check floats for having only 1 digit ftar the dot
            if (field.Contains('.') && decimal.TryParse(field, out decimal result))
            {
                if (decimal.Round(result, 1) != result)
                {
                    throw new ArgumentException("incorrect number of decimal digits");
                }
            }
        }

        //[Test]
        public void CanLogSourceContext()
        {
            //var outputTemplate = "[{Timestamp:HH:mm:ss,fff} {Level:u3}] {Message:lj} [{"+LogCtx.SRC+"}]{NewLine}{Exception}";
            var outputTemplate = "[{Timestamp:HH:mm:ss,fff} {Level:u3}] {Message:lj} [{" + LogCtx.METHOD + "}]{NewLine}{Exception}";
            var _log = new Serilog.LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console(Serilog.Events.LogEventLevel.Verbose, outputTemplate: outputTemplate, theme: AnsiConsoleTheme.Code)
                .WriteTo.Seq("http://localhost:5341")
                .CreateLogger()
                ;
            using var a = LogCtx.Set(methodNameLogLevel: LogEventLevel.Warning);
            _log.Write(Serilog.Events.LogEventLevel.Fatal, "Fatal...");
            var index = 42;
            _log.Error(new ArgumentOutOfRangeException("dummyParamName", "Out of range"), $"Answer {index}");
        }

        //[Test]
        public void CanLogPrivateVersion()
        {
            using var a = LogCtx.Set(methodNameLogLevel: LogEventLevel.Warning);
            Log.Information("Here!");
        }

        //[Test]
        public void CanLogUsingSetupFixture()
        {
            using var a = LogCtx.Set(methodNameLogLevel: LogEventLevel.Warning);
            Log.Warning("CanLogUsingSetupFixture");
            using var b = LogCtx.Set(methodNameLogLevel: LogEventLevel.Warning);
            Log.Fatal(LogCtx.Link() + "error");
            var index = 42;
            Log.Error(new ArgumentOutOfRangeException("dummyParamName", "Out of range"), $"Answer {index}");
        }

        [Test]
        public void CanLogRNetPacket()
        {
            using var a = LogCtx.Set(methodNameLogLevel: LogEventLevel.Warning);
            var received = new RNetPacket(Config.BLOG_ID, Config.DESTINATION_ID, RxTypeEnum.Request);
            Log.Information("JSON@   : {@Received}", received);
            Log.Information("ToString: {$Received}", received);
            Log.Information("Default : {Received}", received);
            var d = new LogCtx.Props() {
                { "Received_dict", JsonConvert.SerializeObject(received)}
            };
            d.AddJson("Received as JSON", received);
            using var b = LogCtx.Set(d);
            Log.Information("The most important things are not visible");
        }

        [Test]
        public void CanReadConfig()
        {
            using var a = LogCtx.Set(methodNameLogLevel: LogEventLevel.Warning);
            Config.InitConfiguration();
        }
    }
}