# Security Policy

Altcha.Net touches form validation and anti-abuse flows. Please report security issues responsibly.

## Reporting a vulnerability

Do not open a public GitHub issue for vulnerabilities, secrets, private payloads or production traces.

Report vulnerabilities privately through GitHub Security Advisories for the repository, or contact the maintainer privately if advisories are unavailable.

When reporting, include:

- affected package version
- target framework and hosting model
- minimal reproduction steps
- expected and observed behavior
- relevant logs with secrets and payloads redacted

## Operational security notes

- Keep `SecretKey` only on the server.
- Never expose `SecretKey` in HTML, JavaScript, logs, telemetry or client-side configuration.
- Use HTTPS in production.
- Keep challenge expiration short.
- Do not log full ALTCHA payloads from production forms.
- Rotate `SecretKey` if it may have been exposed.

## Replay store notes

`MemoryAltchaReplayStore` is thread-safe for one application instance, but it is not suitable for multi-instance, load-balanced or serverless deployments.

For multi-instance ASP.NET Core deployments, use a shared replay store such as `DistributedCacheAltchaReplayStore`, and understand the atomicity guarantees of the underlying `IDistributedCache` provider.

`IDistributedCache` does not expose a universal atomic "add if absent" operation. Some providers can still allow a small replay race under concurrent validation. A dedicated Redis store using `SET NX` is the recommended future direction when strict distributed atomicity is required.
