# LogCtx → NLog-Native Migration Guide

This guide explains how to migrate from the legacy LogCtx abstraction layer to the NLog-native approach based on `ILogger<T>`, message templates, and `BeginScope`.

## Goals of the migration

- Remove custom logging abstractions that hide structured logging from tools.
- Keep the "Set pattern" (scoped context properties) using `ILogger.BeginScope(...)`.
- Ensure properties flow to structured sinks like SEQ.

## Prerequisites

- .NET SDK installed (project target framework applies).
- NLog configured with `IncludeScopes="true"` (or equivalent provider option).
- A config file (e.g., `NLog.config`) copied to the output directory if you load from file.

## Dependency changes (typical)

Update or add packages as appropriate for your project:

- `NLog`
- `NLog.Extensions.Logging`
- `NLog.Targets.Seq` (if using SEQ)
- `Microsoft.Extensions.Logging.Abstractions`
- `Newtonsoft.Json` (if using `Props.AddJson(...)`)

## Namespace changes (typical)

- Remove old LogCtx namespaces (legacy abstraction).
- Use:
  - `Microsoft.Extensions.Logging`
  - `LogCtxShared` (for `Props`, `SetContext`, `LogContextKeys`, helpers)

## API mapping

| Legacy pattern | NLog-native pattern | Notes |
|---|---|---|
| `ILogCtxLogger logger` | `ILogger<MyType> logger` | Use DI injection. |
| `LogCtx.Set(props)` or `logger.Ctx.Set(props)` | `using logger.SetContext(props)` | `SetContext` is scope-based (`IDisposable`). |
| `LogCtx.Set("Operation", "...")` | `using logger.SetOperationContext("...", ("K","V"))` | Use operation name + additional properties. |
| `logger.Info("Text {X}", x)` | `logger.LogInformation("Text {X}", x)` | Message templates preserved. |
| "Context hidden in message text" | "Context in scope properties" | Prefer scope for correlation/querying. |

## Step-by-step migration checklist

1. **Replace logger injection**
   - Before: inject `ILogCtxLogger`
   - After: inject `ILogger<T>`

2. **Replace "Set" usage**
   - Use `using var _ = logger.SetContext(props);`
   - Keep logging calls inside the scope.

3. **Convert context building to `Props`**
   - Use `new Props().Add("Key", value)` or strongly-named keys if you have them.
   - Prefer simple scalar values for best query experience in sinks.

4. **Keep structured message templates**
   - Use `logger.LogInformation("Processing {OrderId} for {CustomerId}", orderId, customerId);`
   - Do not stringify objects just to log them.

5. **Update configuration**
   - Ensure scopes are captured:
     - NLog provider options: `IncludeScopes = true`
     - NLog config uses targets that preserve properties for structured sinks.

6. **Validate in SEQ**
   - Confirm scope properties appear as event properties.
   - Query by keys (e.g., `OrderId`, `Operation`, `CTXSTRACE`).

## Common pattern conversions

### Converting a "set once, log many" block

- Create a `Props` dictionary once.
- Wrap the whole operation with `using logger.SetContext(props)`.

### Nested scopes

- Outer scope: high-level operation context.
- Inner scope: per-item context (e.g., per record, per request, per UI action).

## Configuration migration

### NLog provider options

Ensure you configure the NLog provider with scope capture enabled:

```

logging.AddNLog(new NLogProviderOptions
{
IncludeScopes = true,
CaptureMessageTemplates = true,
CaptureMessageProperties = true
});

```

### NLog.config targets

Confirm your SEQ target includes scope property capture:

```

<target xsi:type="Seq" name="seq" serverUrl="${var:seqUrl}" apiKey="${var:seqApiKey}">
  <property name="MachineName" value="${machinename}" />
  <property name="ProcessId" value="${processid}" />
  <property name="ThreadId" value="${threadid}" />
  <!-- BeginScope properties are automatically captured -->
</target>
```

Enable async logging for performance:

```

<targets async="true">
  <!-- targets here -->
</targets>
```

### Copy config files to output

Ensure NLog.config is copied to the output directory:

```

<ItemGroup>
  <None Update="NLog.config">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

## Troubleshooting

### Scope properties not visible in SEQ

**Symptom:** Properties added via `SetContext` do not appear in SEQ event properties.

**Solution:**
- Confirm NLog provider options include `IncludeScopes = true`.
- Confirm the SEQ target is configured and enabled by rules in `NLog.config`.
- Confirm you are not filtering out the event level you are testing (check `minlevel` in rules).
- Restart the logger factory after configuration changes.

### Properties appear but not queryable

**Symptom:** Properties appear in SEQ logs but queries like `OrderId = 123` return no results.

**Solution:**
- Ensure keys are simple strings and values are scalars where possible.
- Avoid embedding everything into one JSON string unless necessary.
- Use `Props.Add("Key", value)` instead of `Props.AddJson("Key", complexObject)` for queryable properties.

### NLog.config not found at runtime

**Symptom:** Fallback console logging is active, structured logging disabled.

**Solution:**
- Ensure `NLog.config` has `<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>` in `.csproj`.
- Confirm the file is present in the output directory (`bin/Debug/net9.0/`).
- Use `MauiSetup.AddNLogLogging(...)` which handles fallback gracefully.

### CTXSTRACE property missing

**Symptom:** Stack trace context key (`CTXSTRACE`) is not appearing in logs.

**Solution:**
- Confirm you are using `SetContext` (not raw `BeginScope`), which auto-captures caller info.
- Check that your `Props` dictionary does not already contain `CTXSTRACE` (it would be skipped if present).

### Test logger factory conflicts

**Symptom:** Multiple test fixtures create different logger factories, causing SEQ connection issues or duplicate logs.

**Solution:**
- Use a shared `SetUpFixture` to create a single `ILoggerFactory` for all tests.
- See `Tests/Logging.cs` for the recommended pattern with `OneTimeSetUp` and `OneTimeTearDown`.

### Performance concerns

**Symptom:** Logging is slower after migration.

**Solution:**
- Enable async logging in `NLog.config`: `<targets async="true">`.
- Prefer scopes for correlation and structured queries (they are optimized in NLog 5+).
- Keep stack trace capture limited to where it is needed (it is auto-captured per scope, but filtered efficiently).
- Benchmark with `BenchmarkDotNet` if migrating high-throughput code.

### Android/MAUI specific issues

**Symptom:** Logs do not appear in Android logcat or SEQ from MAUI app.

**Solution:**
- Use `MauiSetup.AddNLogLogging(builder.Logging, builder.Configuration)` in `MauiProgram.cs`.
- Add `NLog.Targets.MauiLog` package for Android logcat integration.
- Confirm SEQ URL is accessible from the device/emulator (use device IP for physical devices).

## Performance comparison

### Before (legacy LogCtx)

- Custom abstraction layer adds indirection.
- Context properties hidden in message text or custom storage.
- SEQ queries require full-text search (slower).

### After (NLog-native)

- Direct `ILogger<T>` calls (no abstraction overhead).
- Context properties in scope (optimized by NLog provider).
- SEQ queries use indexed properties (faster, more expressive).
- Async targets enabled by default in recommended config.

### Benchmarking

Use `BenchmarkDotNet` to compare before/after patterns:

```

[Benchmark]
public void LegacyLogCtxSet()
{
LogCtx.Set(new Props().Add("UserId", 123));
_logger.Info("Action");
}

[Benchmark]
public void NativeSetContext()
{
using (_logger.SetContext(new Props().Add("UserId", 123)))
{
_logger.LogInformation("Action");
}
}

```

Expected improvement: 5-15% faster due to reduced abstraction layers and better NLog scope optimization.

## Testing approach

- Unit test `SetContext`:
  - Returns a disposable scope.
  - Scope can be nested.
- Integration test with SEQ (optional):
  - Verify properties arrive and are queryable.
  - Verify stack trace key (e.g., `CTXSTRACE`) appears when expected.

See `Tests/BasicTests.cs` and `Tests/SeqIntegrationTests.cs` for reference test patterns.
