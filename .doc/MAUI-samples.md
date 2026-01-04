# MAUI-samples.md – adjust SetContext usage to new pattern

### Only the relevant code snippets need change (rest of file ok).

#### App.xaml.cs snippet → use `using Props p = ...`

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

        using Props p = _logger.SetContext()
            .Add("Lifecycle", "AppStartup");
        {
            _logger.LogInformation("MauiSample application starting");
        }

        MainPage = mainPage;
    }
}
```


#### MainPage.xaml.cs snippets

Constructor:

```csharp
public MainPage(ILogger<MainPage> logger, SampleService sampleService)
{
    InitializeComponent();

    _logger = logger;
    _sampleService = sampleService;

    using Props p = _logger.SetContext()
        .Add("Page", nameof(MainPage))
        .Add("Lifecycle", "Constructor");
    {
        _logger.LogInformation("MainPage initialized");
    }
}
```

OnAppearing:

```csharp
protected override void OnAppearing()
{
    base.OnAppearing();

    using Props p = _logger.SetContext()
        .Add("Page", nameof(MainPage))
        .Add("Lifecycle", "OnAppearing");
    {
        _logger.LogInformation("MainPage appearing");
    }
}
```

OnCounterClicked:

```csharp
private void OnCounterClicked(object sender, EventArgs e)
{
    _count++;

    using Props p = _logger.SetContext()
        .Add("Page", nameof(MainPage))
        .Add("UiAction", "ButtonClick")
        .Add("Button", "CounterBtn")
        .Add("ClickCount", _count);
    {
        _logger.LogInformation("Counter button clicked");

        CounterBtn.Text = _count == 1
            ? $"Clicked {_count} time"
            : $"Clicked {_count} times";

        StatusLabel.Text = $"Logged click #{_count} to SEQ";

        SemanticScreenReader.Announce(CounterBtn.Text);
    }
}
```

OnProcessClicked:

```csharp
private void OnProcessClicked(object sender, EventArgs e)
{
    using Props p = _logger.SetContext()
        .Add("Page", nameof(MainPage))
        .Add("UiAction", "ProcessData");
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
```

OnErrorClicked:

```csharp
private void OnErrorClicked(object sender, EventArgs e)
{
    using Props p = _logger.SetContext()
        .Add("Page", nameof(MainPage))
        .Add("UiAction", "ThrowException")
        .Add("TestError", true);
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
```


#### SampleService.cs – keep, just consistent style

Already uses `SetOperationContext` + `SetContext(new Props()...)`; you can optionally move to `using Props p = logger.SetContext()` style, but it’s functionally fine.

***
