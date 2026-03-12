# LogCtx Troubleshooting

## I called `SetContext()` without `.Add(...)`. Is that wrong?

No. That is valid usage.

`SetContext()` still captures caller/source context automatically.

## Why is the helper called `MauiSetup` if I am not using MAUI?

Because the name is historical.

It is a bootstrap helper and can still be used from Avalonia/Desktop code if it gives you the logger factory you need.

## Should my main project reference NLog directly?

Prefer not to in normal runtime code.

Use `ILogger<T>` + `LogCtx` in services, view models, and application logic. Keep direct NLog usage limited to bootstrap/config only if necessary.

## Should I use `new Props(...)`?

Not in normal application code.

Use `logger.SetContext()` and then chain `.Add(...)` calls.

## My logs are missing useful context

Check the following:

- Are you opening a scope with `SetContext()`?
- Are you logging inside that scope?
- Are useful stable properties added with `.Add(...)`?
- Is your NLog config capturing scope properties into the target you care about?

## What should I add as structured properties?

Good examples:

- `Operation`
- `LogPath`
- `PayloadLength`
- `ItemCount`
- `Turn`
- `Status`

Bad examples:

- giant blobs that should be logged as the message or separate payload
- constantly changing noise with no diagnostic value
- random properties added “just because structured logging sounds cool”

## Should I keep one long-lived context open for the whole app?

Usually no.

Prefer short-lived scopes for real operations. They are easier to understand and produce cleaner logs.
