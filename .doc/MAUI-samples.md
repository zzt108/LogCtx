## Change Summary

| \# | File | Method | Type | Why |
| :-- | :-- | :-- | :-- | :-- |
| 1 | Samples/MauiSample/MauiSample.csproj | N/A | ✅ NEW | MAUI project file referencing LogCtxShared, NLog packages. [^1] |
| 2 | Samples/MauiSample/MauiProgram.cs | CreateMauiApp | ✅ NEW | Configure DI, NLog, SEQ per Phase 3.2.2. [^1] |
| 3 | Samples/MauiSample/App.xaml.cs | Constructor | ✅ NEW | App entry point with logger injection. [^1] |
| 4 | Samples/MauiSample/MainPage.xaml | N/A | ✅ NEW | Simple UI with button for click event logging. [^1] |
| 5 | Samples/MauiSample/MainPage.xaml.cs | OnClicked | ✅ NEW | Page with SetContext in button handler. [^1] |
| 6 | Samples/MauiSample/Services/SampleService.cs | ProcessData | ✅ NEW | Service with operation-scoped logging. [^1] |
| 7 | Samples/MauiSample/README.md | N/A | ✅ NEW | How to run, build, deploy sample. [^1] |

## Individual Changes

### ✅ NEW — Samples/MauiSample/MauiSample.csproj

**What:** MAUI project file referencing shared LogCtx code and NLog packages.[^1]
**Search for:** (new file) `Samples/MauiSample/MauiSample.csproj`[^1]

✅ FULL FILE VERSION [Samples/MauiSample/MauiSample.csproj]

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFrameworks>net9.0-android</TargetFrameworks>
    <OutputType>Exe</OutputType>
    <RootNamespace>MauiSample</RootNamespace>
    <UseMaui>true</UseMaui>
    <SingleProject>true</SingleProject>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>

    <!-- Android -->
    <SupportedOSPlatformVersion Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'android'">21.0</SupportedOSPlatformVersion>
  </PropertyGroup>

  <ItemGroup>
    <!-- App Icon -->
    <MauiIcon Include="Resources\AppIcon\appicon.svg" ForegroundFile="Resources\AppIcon\appiconfg.svg" Color="#512BD4" />

    <!-- Splash Screen -->
    <MauiSplashScreen Include="Resources\Splash\splash.svg" Color="#512BD4" BaseSize="128,128" />

    <!-- Fonts -->
    <MauiFont Include="Resources\Fonts\*" />
  </ItemGroup>

  <ItemGroup>
    <!-- NLog configuration -->
    <None Update="NLog.config">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
    <None Update="appsettings.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>

  <Import Project="..\..\LogCtxShared\LogCtxShared.projitems" Label="Shared" />

</Project>
```


### ✅ NEW — Samples/MauiSample/MauiProgram.cs

**What:** Configure MAUI builder with DI, NLog, and SEQ using `MauiSetup.AddNLogLogging`.[^1]
**Search for:** (new file) `Samples/MauiSample/MauiProgram.cs`[^1]

✅ FULL FILE VERSION [Samples/MauiSample/MauiProgram.cs]

```csharp
using Microsoft.Extensions.Logging;
using LogCtxShared;
using MauiSample.Services;

namespace MauiSample;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Configure NLog with SEQ
        // For Android emulator: use 10.0.2.2:5341 (emulator's host loopback)
        // For physical device: use your machine's IP (e.g., 192.168.1.100:5341)
        builder.Logging.AddNLogLogging(
            builder.Configuration,
            seqUrl: "http://10.0.2.2:5341", // Android emulator default
            apiKey: null,
            nlogConfigFileName: "NLog.config");

        // Register services
        builder.Services.AddSingleton<SampleService>();

        // Register pages
        builder.Services.AddTransient<MainPage>();

        return builder.Build();
    }
}
```


### ✅ NEW — Samples/MauiSample/App.xaml.cs

**What:** App class with logger injection demonstrating app lifecycle logging.[^1]
**Search for:** (new file) `Samples/MauiSample/App.xaml.cs`[^1]

✅ FULL FILE VERSION [Samples/MauiSample/App.xaml.cs]

```csharp
using Microsoft.Extensions.Logging;
using LogCtxShared;

namespace MauiSample;

public partial class App : Application
{
    private readonly ILogger<App> _logger;

    public App(ILogger<App> logger, MainPage mainPage)
    {
        InitializeComponent();

        _logger = logger;

        using (_logger.SetContext(new Props().Add("Lifecycle", "AppStartup")))
        {
            _logger.LogInformation("MauiSample application starting");
        }

        MainPage = mainPage;
    }
}
```


### ✅ NEW — Samples/MauiSample/App.xaml

**What:** App XAML markup (minimal).[^1]
**Search for:** (new file) `Samples/MauiSample/App.xaml`[^1]

✅ FULL FILE VERSION [Samples/MauiSample/App.xaml]

```xml
<?xml version = "1.0" encoding = "UTF-8" ?>
<Application xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:local="clr-namespace:MauiSample"
             x:Class="MauiSample.App">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="Resources/Styles/Colors.xaml" />
                <ResourceDictionary Source="Resources/Styles/Styles.xaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```


### ✅ NEW — Samples/MauiSample/MainPage.xaml

**What:** Simple page UI with button and label for demonstrating logging.[^1]
**Search for:** (new file) `Samples/MauiSample/MainPage.xaml`[^1]

✅ FULL FILE VERSION [Samples/MauiSample/MainPage.xaml]

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="MauiSample.MainPage">

    <ScrollView>
        <VerticalStackLayout
            Padding="30,0"
            Spacing="25">

            <Image
                Source="dotnet_bot.png"
                HeightRequest="185"
                Aspect="AspectFit"
                SemanticProperties.Description="Cute dot net bot waving hi to you!" />

            <Label
                Text="NLog-Native Logging Sample"
                Style="{StaticResource Headline}"
                SemanticProperties.HeadingLevel="Level1" />

            <Label
                Text="Click the button to test structured logging with context."
                Style="{StaticResource SubHeadline}"
                SemanticProperties.HeadingLevel="Level2"
                SemanticProperties.Description="Click the button to test structured logging with context" />

            <Button
                x:Name="CounterBtn"
                Text="Click me"
                SemanticProperties.Hint="Logs a click event with context"
                Clicked="OnCounterClicked"
                HorizontalOptions="Fill" />

            <Button
                x:Name="ProcessBtn"
                Text="Process Data"
                SemanticProperties.Hint="Calls service to demonstrate operation-scoped logging"
                Clicked="OnProcessClicked"
                HorizontalOptions="Fill" />

            <Button
                x:Name="ErrorBtn"
                Text="Throw Exception"
                SemanticProperties.Hint="Demonstrates exception logging with context"
                Clicked="OnErrorClicked"
                HorizontalOptions="Fill"
                BackgroundColor="Red" />

            <Label
                x:Name="StatusLabel"
                Text="Ready"
                HorizontalOptions="Center" />

        </VerticalStackLayout>
    </ScrollView>

</ContentPage>
```


### ✅ NEW — Samples/MauiSample/MainPage.xaml.cs

**What:** Code-behind with SetContext in button handlers showing page lifecycle and user action logging.[^1]
**Search for:** (new file) `Samples/MauiSample/MainPage.xaml.cs`[^1]

✅ FULL FILE VERSION [Samples/MauiSample/MainPage.xaml.cs]

```csharp
using Microsoft.Extensions.Logging;
using LogCtxShared;
using MauiSample.Services;

namespace MauiSample;

public partial class MainPage : ContentPage
{
    private readonly ILogger<MainPage> _logger;
    private readonly SampleService _sampleService;
    private int _count = 0;

    public MainPage(ILogger<MainPage> logger, SampleService sampleService)
    {
        InitializeComponent();

        _logger = logger;
        _sampleService = sampleService;

        using (_logger.SetContext(new Props()
            .Add("Page", nameof(MainPage))
            .Add("Lifecycle", "Constructor")))
        {
            _logger.LogInformation("MainPage initialized");
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        using (_logger.SetContext(new Props()
            .Add("Page", nameof(MainPage))
            .Add("Lifecycle", "OnAppearing")))
        {
            _logger.LogInformation("MainPage appearing");
        }
    }

    private void OnCounterClicked(object sender, EventArgs e)
    {
        _count++;

        using (_logger.SetContext(new Props()
            .Add("Page", nameof(MainPage))
            .Add("UiAction", "ButtonClick")
            .Add("Button", "CounterBtn")
            .Add("ClickCount", _count)))
        {
            _logger.LogInformation("Counter button clicked");

            CounterBtn.Text = _count == 1
                ? $"Clicked {_count} time"
                : $"Clicked {_count} times";

            StatusLabel.Text = $"Logged click #{_count} to SEQ";

            SemanticScreenReader.Announce(CounterBtn.Text);
        }
    }

    private void OnProcessClicked(object sender, EventArgs e)
    {
        using (_logger.SetContext(new Props()
            .Add("Page", nameof(MainPage))
            .Add("UiAction", "ProcessData")))
        {
            _logger.LogInformation("Process button clicked, calling service");

            try
            {
                var result = _sampleService.ProcessData(orderId: 12345, customerId: 67890);
                StatusLabel.Text = result;
                _logger.LogInformation("Service call completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Service call failed");
                StatusLabel.Text = $"Error: {ex.Message}";
            }
        }
    }

    private void OnErrorClicked(object sender, EventArgs e)
    {
        using (_logger.SetContext(new Props()
            .Add("Page", nameof(MainPage))
            .Add("UiAction", "ThrowException")
            .Add("TestError", true)))
        {
            _logger.LogWarning("User triggered test exception");

            try
            {
                throw new InvalidOperationException("This is a test exception to demonstrate error logging with context");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Test exception caught and logged");
                StatusLabel.Text = "Exception logged to SEQ";
            }
        }
    }
}
```


### ✅ NEW — Samples/MauiSample/Services/SampleService.cs

**What:** Service demonstrating operation-scoped logging with nested contexts.[^1]
**Search for:** (new file) `Samples/MauiSample/Services/SampleService.cs`[^1]

✅ FULL FILE VERSION [Samples/MauiSample/Services/SampleService.cs]

```csharp
using Microsoft.Extensions.Logging;
using LogCtxShared;

namespace MauiSample.Services;

public class SampleService
{
    private readonly ILogger<SampleService> _logger;

    public SampleService(ILogger<SampleService> logger)
    {
        _logger = logger;
    }

    public string ProcessData(int orderId, int customerId)
    {
        using (_logger.SetOperationContext(
            "ProcessOrder",
            ("OrderId", orderId),
            ("CustomerId", customerId)))
        {
            _logger.LogInformation("Starting order processing");

            // Simulate some processing steps
            ValidateOrder(orderId);
            CalculateTotal(orderId);
            SubmitOrder(orderId);

            _logger.LogInformation("Order processing completed");

            return $"Order {orderId} processed successfully";
        }
    }

    private void ValidateOrder(int orderId)
    {
        using (_logger.SetContext(new Props()
            .Add("Step", "Validation")
            .Add("OrderId", orderId)))
        {
            _logger.LogDebug("Validating order");
            // Simulate validation
            Thread.Sleep(100);
            _logger.LogDebug("Order validation passed");
        }
    }

    private void CalculateTotal(int orderId)
    {
        using (_logger.SetContext(new Props()
            .Add("Step", "Calculation")
            .Add("OrderId", orderId)))
        {
            _logger.LogDebug("Calculating order total");
            // Simulate calculation
            var total = 123.45m;
            _logger.LogInformation("Order total calculated: {Total}", total);
        }
    }

    private void SubmitOrder(int orderId)
    {
        using (_logger.SetContext(new Props()
            .Add("Step", "Submission")
            .Add("OrderId", orderId)))
        {
            _logger.LogDebug("Submitting order to backend");
            // Simulate submission
            Thread.Sleep(100);
            _logger.LogInformation("Order submitted successfully");
        }
    }
}
```


### ✅ NEW — Samples/MauiSample/NLog.config

**What:** NLog config for MAUI with SEQ target and Android-friendly settings.[^1]
**Search for:** (new file) `Samples/MauiSample/NLog.config`[^1]

✅ FULL FILE VERSION [Samples/MauiSample/NLog.config]

```xml
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
      autoReload="true"
      throwConfigExceptions="true">

  <!-- Variables for configuration -->
  <variable name="seqUrl" value="http://10.0.2.2:5341"/>
  <variable name="seqApiKey" value=""/>

  <!-- Targets for logging -->
  <targets async="true">
    <!-- Console target for debugging -->
    <target xsi:type="Console" name="console"
            layout="${longdate}|${level:uppercase=true}|${logger}|${message}${onexception:inner=${newline}${exception:format=toString}}"/>

    <!-- SEQ target for structured logging -->
    <target xsi:type="Seq" name="seq" serverUrl="${var:seqUrl}" apiKey="${var:seqApiKey}">
      <!-- Capture machine/process context -->
      <property name="MachineName" value="${machinename}" />
      <property name="ProcessId" value="${processid}" />
      <property name="ThreadId" value="${threadid}" />
      <!-- BeginScope properties are automatically captured -->
    </target>
  </targets>

  <!-- Logging rules -->
  <rules>
    <!-- Console: Debug and above -->
    <logger name="*" minlevel="Debug" writeTo="console"/>

    <!-- SEQ: Trace and above for full visibility -->
    <logger name="*" minlevel="Trace" writeTo="seq"/>
  </rules>
</nlog>
```


### ✅ NEW — Samples/MauiSample/appsettings.json

**What:** App settings for SEQ connection (optional override).[^1]
**Search for:** (new file) `Samples/MauiSample/appsettings.json`[^1]

✅ FULL FILE VERSION [Samples/MauiSample/appsettings.json]

```json
{
  "Seq": {
    "Url": "http://10.0.2.2:5341",
    "ApiKey": "",
    "MinimumLevel": "Trace"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "MauiSample": "Trace"
    }
  }
}
```


### ✅ NEW — Samples/MauiSample/README.md

**What:** Instructions for building, running, and verifying the sample app.[^1]
**Search for:** (new file) `Samples/MauiSample/README.md`[^1]

✅ FULL FILE VERSION [Samples/MauiSample/README.md]

```md
# MauiSample - NLog-Native Logging Example

Complete MAUI Android sample demonstrating NLog-native structured logging with SEQ integration.

## Features

- `ILogger<T>` dependency injection in App, Pages, and Services
- `SetContext` for scoped context properties (page lifecycle, UI actions)
- `SetOperationContext` for operation-scoped logging (service methods)
- Automatic `CallerInfo` capture (file, method, line)
- SEQ structured logging with queryable properties
- Exception logging with context
- Nested scope demonstration

## Prerequisites

1. **.NET 9 SDK** with MAUI workload:
```

dotnet workload install maui

```

2. **SEQ** running locally:
```


# Docker (easiest)

docker run -d --name seq -e ACCEPT_EULA=Y -p 5341:80 datalust/seq:latest

# Or Windows/Mac installer from https://datalust.co/seq

```

3. **Android emulator** or physical device

## Build & Run

### Android Emulator

```

cd Samples/MauiSample
dotnet build -t:Run -f net9.0-android

```

### Physical Android Device

1. Update SEQ URL in `MauiProgram.cs`:
```

seqUrl: "http://YOUR_MACHINE_IP:5341"  // e.g., "http://192.168.1.100:5341"

```

2. Enable USB debugging on device

3. Run:
```

dotnet build -t:Run -f net9.0-android

```

## Verify Logging in SEQ

1. Open **http://localhost:5341** in browser

2. You should see events with:
- **CTXSTRACE** property (source tracking)
- **Page**, **UiAction** properties (from MainPage)
- **Operation**, **OrderId**, **CustomerId** (from SampleService)
- **Step** property (nested scopes in service)

3. Try these queries:
```

Page = 'MainPage'
UiAction = 'ButtonClick'
Operation = 'ProcessOrder'
OrderId = 12345
@Exception is not null

```

## Sample Code Highlights

### App Lifecycle Logging
```

// App.xaml.cs
using (_logger.SetContext(new Props().Add("Lifecycle", "AppStartup")))
{
_logger.LogInformation("MauiSample application starting");
}

```

### UI Action Logging
```

// MainPage.xaml.cs
using (_logger.SetContext(new Props()
.Add("Page", nameof(MainPage))
.Add("UiAction", "ButtonClick")
.Add("ClickCount", _count)))
{
_logger.LogInformation("Counter button clicked");
}

```

### Service Operation Logging
```

// Services/SampleService.cs
using (_logger.SetOperationContext(
"ProcessOrder",
("OrderId", orderId),
("CustomerId", customerId)))
{
_logger.LogInformation("Starting order processing");
// ... nested scopes for validation, calculation, submission
}

```

## Troubleshooting

### SEQ not receiving logs

- **Emulator:** Confirm SEQ URL is `http://10.0.2.2:5341` (emulator's host loopback)
- **Physical device:** Use your machine's IP address, ensure firewall allows port 5341
- Check SEQ is running: `docker ps` or open http://localhost:5341

### App crashes on startup

- Ensure NLog.config is copied to output (check .csproj)
- Check NuGet packages are restored
- Review Android logcat output: `adb logcat *:E`

### Properties not appearing in SEQ

- Confirm `IncludeScopes = true` in `MauiProgram.cs`
- Verify NLog.config has SEQ target enabled
- Check SEQ minimum level allows Trace/Debug

## Project Structure

```

MauiSample/
├── MauiProgram.cs          \# DI + NLog setup
├── App.xaml.cs             \# App lifecycle logging
├── MainPage.xaml           \# UI markup
├── MainPage.xaml.cs        \# Page with SetContext
├── Services/
│   └── SampleService.cs    \# Service with operation logging
├── NLog.config             \# NLog configuration
├── appsettings.json        \# SEQ connection settings
└── README.md               \# This file

```

## Next Steps

- Explore SEQ queries with structured properties
- Add custom properties for your domain (e.g., UserId, TenantId)
- Try nested scopes for complex workflows
- Review `CTXSTRACE` property to see source code locations
```
