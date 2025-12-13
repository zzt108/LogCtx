using Microsoft.Extensions.Logging;
using NUnit.Framework;
using LogCtxShared;

[SetUpFixture]
public sealed class Logging
{
    public static ILoggerFactory Factory { get; private set; } = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        Factory = MauiSetup.CreateLoggerFactory(
            configuration: null,
            seqUrl: "http://localhost:5341",
            apiKey: null,
            nlogConfigFileName: "NLog.config");
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        Factory.Dispose();
        NLog.LogManager.Shutdown();
    }
}