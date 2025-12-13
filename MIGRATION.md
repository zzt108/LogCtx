# LogCtx → NLog-Native Migration Guide

This guide explains how to migrate from the legacy LogCtx abstraction layer to the NLog-native approach based on `ILogger<T>`, message templates, and `BeginScope`.

## Goals of the migration

- Remove custom logging abstractions that hide structured logging from tools.
- Keep the “Set pattern” (scoped context properties) using `ILogger.BeginScope(...)`.
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
| “Context hidden in message text” | “Context in scope properties” | Prefer scope for correlation/querying. |

## Step-by-step migration checklist

1. **Replace logger injection**
   - Before: inject `ILogCtxLogger`
   - After: inject `ILogger<T>`

2. **Replace “Set” usage**
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

### Converting a “set once, log many” block

- Create a `Props` dictionary once.
- Wrap the whole operation with `using logger.SetContext(props)`.

### Nested scopes

- Outer scope: high-level operation context.
- Inner scope: per-item context (e.g., per record, per request, per UI action).

## Troubleshooting

### Scope properties not visible in SEQ
- Confirm NLog provider options include scopes.
- Confirm the SEQ target is configured and enabled by rules.
- Confirm you are not filtering out the event level you are testing.

### Properties appear but not queryable
- Ensure keys are simple strings and values are scalars where possible.
- Avoid embedding everything into one JSON string unless necessary.

### Performance concerns
- Prefer scopes for correlation and structured queries.
- Keep stack trace capture limited to where it is needed.

## Testing approach

- Unit test `SetContext`:
  - Returns a disposable scope.
  - Scope can be nested.
- Integration test with SEQ (optional):
  - Verify properties arrive and are queryable.
  - Verify stack trace key (e.g., `CTXSTRACE`) appears when expected.
