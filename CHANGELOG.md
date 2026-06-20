# Changelog

## Unreleased

### Security / Hardening

- Added a configurable maximum ALTCHA payload length to reject oversized payloads before Base64 decoding.
- Added a strict distributed replay protection option for multi-instance deployments.
- Made validation time injectable internally to keep expiry-related tests deterministic.
- Cleaned unsupported algorithm error messages.

### Maintenance

- Centralized common MSBuild properties and NuGet package versions.
- Simplified solution configurations to `Debug|Any CPU` and `Release|Any CPU`.

### Tests

- Added deterministic ALTCHA interoperability fixtures for standard and URL-safe Base64 payloads.
- Extended ASP.NET Core integration tests to cover both net8.0 and net10.0.
- Strengthened validation edge-case coverage.

### Features

- Added an optional Redis-backed atomic replay store for ASP.NET Core deployments using Redis `SET NX` with expiration, addressing the "Add atomic Redis replay store for ALTCHA validation" request.
- Added the optional `Altcha.Net.AspNetCore` integration package with DI registration, Minimal API challenge mapping and an `IDistributedCache` replay store.
- Added an ASP.NET Core Minimal API example.
- Added additional validation and replay tests.
- Removed the experimental async validation and replay-store API.

### Compatibility

- The core package remains independent from ASP.NET Core dependencies.
- `Altcha.Net.AspNetCore` targets modern .NET only.
- The `.NET Framework 4.8` target no longer depends on `System.Text.Json` or `Microsoft.Bcl.AsyncInterfaces`.
- The `.NET Standard 2.0` target now references `System.Text.Json` `8.0.6`; `.NET 10` uses the shared framework.

### Security notes

- `DistributedCacheAltchaReplayStore` is suitable for shared cache deployments, but `IDistributedCache` does not guarantee atomic insert semantics for every provider.
- `RedisAltchaReplayStore` uses an atomic Redis `SET NX` pattern with expiration for strict replay protection.

### Known limitations

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
