using AIA_Test.Common;
using Newtonsoft.Json;
using RNet;

//namespace AIA_Test
//{
// Must be out of all namespaces to work for all test in the assembly
[SetUpFixture]
public class SetupFixture
{

    [OneTimeSetUp]
    public void SetUpFixture()
    {
        Console.WriteLine("SetUpFixture: Setup test environment");
        Config.InitConfiguration();

        var outputTemplate = $"[{{Timestamp:HH:mm:ss,fff}} {{Level:u3}}] {{Message:lj}} [{{{LogCtx.FILE}}}.{{{LogCtx.METHOD}}}]{{NewLine}}{{Exception}}";
        //var outputTemplate = $"[{{Timestamp:mm:ss,fff}} {{Level:u3}}] {{Message:lj}}{{NewLine}}{{Exception}}";
        Log.Logger = new Serilog.LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.FromLogContext()
            .WriteTo.Console(Serilog.Events.LogEventLevel.Warning, outputTemplate: outputTemplate)
            .WriteTo.Seq("http://localhost:5341")
                .Destructure.ByTransforming<RNetPacket>(rnp => JsonConvert.SerializeObject(rnp))
            .CreateLogger();

        LogCtx.Set();

        // Get serial port configuration and port names
        Log.Verbose("Get serial port configuration and port names");
        foreach (var aiaSerial in (AiaSerial[])Enum.GetValues(typeof(AiaSerial)))
        {
            //Log.Verbose("Testing {AiaPort}", aiaSerial);
            try
            {
                var portName = SerialPorts.GetComPortName(aiaSerial);
                if (portName != null)
                {
                    Config.SetComPortName(aiaSerial, portName);
                    Log.Verbose("Found {Name} {AiaSerial}", portName, aiaSerial);
                }
                else
                    Log.Warning("{AiaPort} port NOT found", aiaSerial);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Port {AiaSerialPort} exception", aiaSerial);
            }
        }
        Config.SerialPortsScanned = true;
    }

    [OneTimeTearDown]
    public void TearDownFixture()
    {
        Console.WriteLine("TearDownFixture: Dispose test environment");
        Log.CloseAndFlush();
    }
}
//}