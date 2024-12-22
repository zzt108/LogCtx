
# LogCtx

**Log Contextualization Library for .NET**

LogCtx is a lightweight .NET library designed to enrich your application logs with contextual information, making debugging and analysis easier. It provides a consistent way to add properties to your log messages, regardless of the underlying logging framework you choose (currently supporting NLog and Serilog). This project is intended to be included as a sub-repository within other projects, with the shared components directly referenced by the host project.

## Functionality

- **Contextual Logging:**  Easily add contextual properties to your logs, such as method name, file name, line number, and custom data.
- **Stack Trace Enrichment:** Automatically captures and includes relevant parts of the stack trace in your logs for better error diagnosis.
- **Pluggable Logging Frameworks:** Supports multiple logging frameworks through adapter implementations. Currently supports:
    - NLog
    - Serilog
- **Simplified Usage:** Provides a fluent API for setting context properties.
- **JSON Serialization Extensions:** Includes handy extensions for serializing objects to JSON, useful for logging complex data structures.
- **`Props` Class:** A dictionary-based class for managing contextual properties, with convenient initialization and disposal.

## Project Structure

The project is organized into the following folders:

- **`LogCtx`:** The root folder containing the solution file and other configuration files.
- **`LogCtxShared`:** Contains the core interfaces and classes that are shared between different logging framework adapters. This project is intended to be directly referenced by the host project.
    - `ILogCtxLogger`: Defines the interface for logging implementations.
    - `IScopeContext`: Defines the interface for managing contextual properties.
    - `LogCtx`: The main class for setting and managing log context.
    - `Props`: A dictionary class for storing log context properties.
    - `JsonExtensions`: Provides extension methods for JSON serialization.
- **`NLogShared`:** Contains the NLog adapter implementation. This project is intended to be directly referenced by the host project.
    - `NLogCtx`: Implements `ILogCtxLogger` for NLog.
    - `NLogScopeContext`: Implements `IScopeContext` for NLog.
- **`SeriLogShared`:** Contains the Serilog adapter implementation. This project is intended to be directly referenced by the host project.
    - `SeriLogCtx`: Implements `ILogCtxLogger` for Serilog.
    - `SeriLogScopeContext`: Implements `IScopeContext` for Serilog.
- **`Old`:**  Contains older versions or alternative implementations (likely deprecated).
- **`SeriLogAdapterTests`:** Contains unit tests for the Serilog adapter.

## Getting Started

### Integration as a Sub-Repository

To integrate LogCtx into your project:

1. **Add as a Submodule:** In your project's root directory, run the following command:
    ```bash
    git submodule add <repository_url> LogCtx
    git submodule init
    git submodule update
    ```
    Replace `<repository_url>` with the URL of the LogCtx repository.

2. **Reference Shared Projects:** In your host project, add project references to the desired shared projects:
    - `LogCtxShared/LogCtxShared.projitems`
    - `NLogShared/NLogShared.csproj` (if using NLog)
    - `SeriLogShared/SeriLogShared.projitems` (if using Serilog)

### Usage

Here's a basic example of how to use LogCtx with the Serilog adapter:

```csharp
using LogCtxShared;
using SeriLogAdapter;

public class MyClass
{
    private readonly ILogCtxLogger _logger;

    public MyClass()
    {
        _logger = new SeriLogCtx();
        _logger.ConfigureJson("Config/LogConfig.json"); // Or ConfigureXml for XML configuration
    }

    public void MyMethod(string data)
    {
        using (_logger.Ctx.Set(new Props { { "data", data } }))
        {
            _logger.Info("Processing data.");
            try
            {
                // Some operation that might throw an exception
                throw new InvalidOperationException("Something went wrong!");
            }
            catch (Exception ex)
            {
                _logger.Fatal(ex, "An error occurred.");
            }
        }
    }
}
```

**Explanation:**

1. **Create a Logger Instance:** Instantiate the desired logger adapter (e.g., `SeriLogCtx`).
2. **Configure the Logger:** Call `ConfigureJson` or `ConfigureXml` to load the logging configuration from a file.
3. **Set Context:** Use `_logger.Ctx.Set()` to establish a logging context. You can pass a `Props` object containing key-value pairs for your contextual information. The `using` statement ensures the context is cleared when the block finishes.
4. **Log Messages:** Use the `_logger` methods (e.g., `Info`, `Error`, `Fatal`) to log messages. The contextual properties set in the `Set()` method will be included in these log messages.
5. **Automatic Source Information:** The `LogCtx` automatically captures the file name, method name, and line number where the `Set()` method is called and includes it in the log context under the keys `CTX_FILE`, `CTX_METHOD`, and `CTX_LINE` (or collectively under `CTX_SRC` and `CTX_STRACE`).

**Using the `Props` class:**

The `Props` class simplifies the creation of contextual properties. You can initialize it with an anonymous object or use the dictionary initializer:

```csharp
using (_logger.Ctx.Set(new Props { { "user_id", 123 }, { "correlation_id", Guid.NewGuid() } }))
{
    _logger.Info("User accessed resource.");
}

using (_logger.Ctx.Set(new Props("important", "value", 42)))
{
    _logger.Debug("More details.");
}
```

**JSON Extensions:**

The `JsonExtensions` class provides helpful methods for serializing objects to JSON for logging:

```csharp
var myObject = new { Id = 1, Name = "Example" };
_logger.Info($"Object details: {myObject.AsJson(true)}"); // Indented JSON
_logger.Debug($"Embedded JSON in PlantUML: {myObject.AsJsonEmbedded()}");
```

## Adapters

### NLog

To use LogCtx with NLog, reference the `NLogShared.csproj` project from your host project and use the `NLogCtx` class:

```csharp
using LogCtxShared;
using NLogAdapter;

public class MyNLogClass
{
    private readonly ILogCtxLogger _logger;

    public MyNLogClass()
    {
        _logger = new NLogCtx();
        _logger.ConfigureXml("NLog.config");
    }

    public void MyMethod()
    {
        using (_logger.Ctx.Set(new Props { { "operation", "calculate" } }))
        {
            _logger.Info("Starting calculation.");
        }
    }
}
```

Make sure you have a valid `NLog.config` file configured for your logging needs within your host project.

### Serilog

To use LogCtx with Serilog, reference the `SeriLogShared.projitems` file from your host project and use the `SeriLogCtx` class (as shown in the Getting Started example).

Ensure your Serilog configuration is set up correctly, either through JSON, XML, or code within your host project.

## Contributing

Contributions to LogCtx are welcome! If you find a bug or have a suggestion for improvement, please open an issue or submit a pull request.

## License

This project is licensed under the Apache License 2.0 - see the [LICENSE](LICENSE-2.0.txt) file for details.