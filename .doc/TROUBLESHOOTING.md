# Troubleshooting

## Scope properties not visible in SEQ
**Symptoms**
- Logs arrive in SEQ but properties set via `SetContext()` do not show up as event properties.

**Fix**
- Ensure scope capture is enabled in the NLog provider options (handled automatically if you use `MauiSetup.AddNLogLogging(...)`).
- Ensure your `NLog.config` rule writes the event to the SEQ target for the level you are testing.
- Make sure you are inside a `using Props p = logger.SetContext() { ... }` scope when logging.

## Properties appear but are not queryable
**Symptoms**
- Properties appear in event details, but queries like `OrderId = 123` do not work reliably.

**Fix**
- Prefer scalar properties: `.Add("OrderId", orderId)` over `.AddJson("Order", orderObject)`.
- Use `AddJson` only for “debug payload” style data, not primary query dimensions.

## NLog.config not found at runtime (MAUI / Android)
**Symptoms**
- App runs but structured logging behaves differently (or only fallback output exists).

**Fix**
- Ensure `NLog.config` is copied to output in the MAUI project:
  - `CopyToOutputDirectory="PreserveNewest"` in the `.csproj`.
- Confirm the file exists under the deployed output.

## CTXSTRACE is missing
**Symptoms**
- You expect `CTXSTRACE`, but it doesn’t show.

**Fix**
- Ensure you are using `logger.SetContext()` (it auto-enriches with CTXSTRACE).
- If you pass a `Props` that already contains `CTXSTRACE`, it will not be overwritten.
- Remember CTXSTRACE is captured at the `SetContext()` call site, not at `Add()`.

## Android emulator can’t reach SEQ
**Symptoms**
- Works on desktop tests, but no events from Android emulator.

**Fix**
- Use emulator host loopback: `http://10.0.2.2:5341` (typical Android emulator mapping).
- Ensure firewall rules allow inbound access to SEQ port 5341.

## Physical device can’t reach SEQ
**Symptoms**
- Emulator works; physical device doesn’t.

**Fix**
- Use the machine’s LAN IP address (not localhost) in `seqUrl`.
- Ensure the device is on the same network and SEQ is reachable.

## Duplicate logs or unstable SEQ integration tests
**Symptoms**
- Tests produce duplicated events or intermittent “wrong configuration” behavior.

**Fix**
- Create a single shared `ILoggerFactory` for the test run (one-time setup) and dispose it once.
- Avoid reloading NLog configuration independently in many fixtures unless needed.

## Performance concerns
**Symptoms**
- Logging feels slower after enabling rich context.

**Fix**
- Ensure async targets are enabled in `NLog.config` (`<targets async="true">`).
- Use `SetContext` scopes around logical operations (not around every tiny line).
- Avoid gigantic payloads or very deep stack traces in ultra-hot paths.
```
