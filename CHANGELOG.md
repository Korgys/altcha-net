# Changelog

## Unreleased

### Features

- Added the optional `Altcha.Net.AspNetCore` integration package with DI registration, Minimal API challenge mapping and an `IDistributedCache` replay store.
- Added an ASP.NET Core Minimal API example.
- Added additional validation and replay tests.

### Compatibility

- The core `Altcha.Net` public API is unchanged.
- The core package remains independent from ASP.NET Core dependencies.
- `Altcha.Net.AspNetCore` targets modern .NET only.

### Security notes

- `DistributedCacheAltchaReplayStore` is suitable for shared cache deployments, but `IDistributedCache` does not guarantee atomic insert semantics for every provider.
- A dedicated Redis implementation using an atomic `SET NX` pattern remains a future improvement.

### Known limitations

- The ASP.NET Core package does not add Redis directly.
- The memory replay store remains single-instance only.

## 1.0.0

### Features

- Initial community implementation of ALTCHA legacy proof-of-work validation for .NET.
- Challenge generation compatible with the ALTCHA widget legacy PoW v1 JSON shape.
- Base64 and URL-safe Base64 payload validation.
- HMAC SHA-256 signature validation.
- Replay detection through `IAltchaReplayStore`.
- Thread-safe in-memory replay store.
- Examples for ASP.NET Framework 4.8 MVC C# and WebForms VB.NET.

### Compatibility

- Targets `net48`, `netstandard2.0` and `net10.0`.
- Keeps the core package independent from ASP.NET Core and external services.

### Security notes

- `SecretKey` must stay server-side and must never be sent to the browser.
- HTTPS is required in production.
- The in-memory replay store is not suitable for multi-instance deployments.
- ALTCHA proof-of-work is one anti-abuse control, not a complete anti-spam or anti-bot solution.

### Known limitations

- No ALTCHA Sentinel integration.
- No ALTCHA spam filter API integration.
- No Redis replay store in the core package.
- SHA-256 proof-of-work only.
- No distributed replay protection in the 1.0.0 core package.
