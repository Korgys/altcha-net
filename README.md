# Altcha.Net

[![NuGet](https://img.shields.io/nuget/v/Altcha.Net.svg)](https://www.nuget.org/packages/Altcha.Net)
[![Build](https://github.com/Korgys/altcha-net/actions/workflows/ci.yml/badge.svg)](https://github.com/Korgys/altcha-net/actions/workflows/ci.yml)
[![License](https://img.shields.io/github/license/Korgys/altcha-net.svg)](LICENSE)

Altcha.Net est une librairie .NET open-source communautaire et non officielle pour utiliser ALTCHA en mode proof-of-work auto-heberge.

Elle ne depend pas d'ALTCHA Sentinel, n'appelle aucune API ALTCHA externe et vise les applications modernes comme les sites legacy ASP.NET Framework 4.8.

Altcha.Net fournit un captcha proof-of-work simple. Ce n'est pas une solution anti-spam ou anti-bot complete.

## Install

```bash
dotnet add package Altcha.Net
```

Package ASP.NET Core optionnel:

```bash
dotnet add package Altcha.Net.AspNetCore
```

## ASP.NET Core quick start

```csharp
using Altcha.Net;
using Altcha.Net.AspNetCore;

builder.Services.AddAltcha(options =>
{
    options.SecretKey = builder.Configuration["Altcha:SecretKey"]!;
    options.ChallengeExpiry = TimeSpan.FromMinutes(2);
    options.AllowedClockSkew = TimeSpan.FromSeconds(10);
    options.Complexity = new AltchaComplexity(50000, 100000);
});

app.MapAltchaChallenge("/altcha/challenge");
```

Pour utiliser un cache partage:

```csharp
builder.Services.AddDistributedMemoryCache();
builder.Services.AddAltcha(builder.Configuration.GetSection("Altcha"));
builder.Services.AddDistributedAltchaReplayStore();
```

Mode atomique strict (recommande en multi-instance) avec un backend qui supporte une operation de type `SET key value NX EX`:

```csharp
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

builder.Services.AddSingleton<IAtomicAltchaReplayStore, RedisAtomicAltchaReplayStore>();
builder.Services.AddAltcha(builder.Configuration.GetSection("Altcha"));
builder.Services.AddDistributedAltchaReplayStore(DistributedAltchaReplayStoreMode.StrictAtomic);
```

`DistributedCacheAltchaReplayStore` utilise `IDistributedCache`. Cette abstraction ne garantit pas une insertion atomique pour tous les providers.

## Endpoint hardening

L'endpoint challenge peut etre durci avec des conventions optionnelles:

```csharp
using Altcha.Net;
using Altcha.Net.AspNetCore;
using System.Threading.RateLimiting;

builder.Services.AddAltcha(builder.Configuration.GetSection("Altcha"));
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("altcha-challenge", limiter =>
    {
        limiter.PermitLimit = 30;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });
});

var app = builder.Build();

app.UseRateLimiter();

app.MapAltchaChallenge("/altcha/challenge", security =>
{
    security.RateLimitingPolicyName = "altcha-challenge";
    security.AllowedHosts = ["example.com", "www.example.com"];
    // Cache-Control: no-store est active par defaut.
});
```

Exemple minimal sans rate limiter (seulement `Cache-Control: no-store`):

```csharp
app.MapAltchaChallenge("/altcha/challenge");
```

## ASP.NET Framework 4.8 quick start

```csharp
using Altcha.Net;

var service = new AltchaService(new AltchaOptions
{
    SecretKey = Environment.GetEnvironmentVariable("ALTCHA_SECRET")!,
    ChallengeExpiry = TimeSpan.FromMinutes(2),
    AllowedClockSkew = TimeSpan.FromSeconds(10),
    Complexity = new AltchaComplexity(50000, 100000)
}, new MemoryAltchaReplayStore());
```

Endpoint challenge MVC:

```csharp
public ActionResult Challenge()
{
    return Content(AltchaProvider.Service.GenerateChallenge().ToJson(), "application/json");
}
```

Validation POST:

```csharp
var result = AltchaProvider.Service.ValidateResponse(Request.Form["altcha"]);
if (!result.IsValid)
{
    ModelState.AddModelError("", "Validation ALTCHA invalide.");
    return View(model);
}
```

Des exemples sont disponibles dans:

- `examples/Altcha.Net.Examples.AspNetMvc.CSharp`
- `examples/Altcha.Net.Examples.AspNetWebForms.VbNet`
- `examples/Altcha.Net.Examples.AspNetCore.MinimalApi`

## Widget HTML

Hebergez le script du widget ALTCHA dans votre application, puis pointez `challenge` vers votre endpoint local.

```html
<script async defer src="/scripts/altcha.min.js" type="module"></script>

<form method="post" action="/Contact/Submit">
  <input name="email" type="email" required>
  <textarea name="message" required></textarea>
  <altcha-widget challenge="/altcha/challenge"></altcha-widget>
  <button type="submit">Envoyer</button>
</form>
```

Le widget poste un champ de formulaire `altcha` contenant un JSON encode en Base64.

## Configuration

- `SecretKey`: cle HMAC serveur. Ne jamais l'exposer au navigateur.
- `ChallengeExpiry`: duree de validite courte, par defaut 2 minutes.
- `Complexity`: plage du nombre proof-of-work, par defaut `50000..100000`.
- `AllowedClockSkew`: marge de tolerance inter-noeuds pour l'expiration, par defaut 10 secondes (recommande entre 5 et 30 secondes avec NTP actif).
- `IAltchaReplayStore`: store anti-replay.
- `Algorithm`: seul `SHA-256` est supporte actuellement.

## Production notes

- Stocker `SecretKey` dans un secret manager ou une variable d'environnement.
- Servir le site en HTTPS.
- Garder une expiration courte.
- Synchroniser les horloges serveurs via NTP (chrony/systemd-timesyncd/Windows Time) pour limiter le skew.
- Ne pas logger les payloads ALTCHA complets ni la cle secrete.
- Utiliser un store partage en multi-instance.
- Eviter `MemoryAltchaReplayStore` en production multi-serveur.
- `AddDistributedAltchaReplayStore(DistributedAltchaReplayStoreMode.BestEffort)` utilise `IDistributedCache` en fallback best effort: anti-replay non strictement atomique.
- `AddDistributedAltchaReplayStore(DistributedAltchaReplayStoreMode.StrictAtomic)` exige `IAtomicAltchaReplayStore` et garantit un "insert-if-absent" atomique entre workers (ex: Redis `SET ... NX EX`).

## Known limitations

- Pas d'integration ALTCHA Sentinel.
- Pas de spam filter API ALTCHA.
- Proof-of-work uniquement.
- SHA-256 uniquement.
- Pas de Redis integre.
- `IDistributedCache` ne fournit pas toujours une atomicite stricte.

## Not affiliated with ALTCHA

Altcha.Net est une implementation communautaire non officielle. Ce projet n'est pas affilie, approuve ou sponsorise par ALTCHA.

## Build, Test, Pack

```bash
dotnet restore Altcha.Net.sln
dotnet build Altcha.Net.sln --configuration Release
dotnet test Altcha.Net.sln --configuration Release
dotnet pack src/Altcha.Net/Altcha.Net.csproj --configuration Release --output artifacts
dotnet pack src/Altcha.Net.AspNetCore/Altcha.Net.AspNetCore.csproj --configuration Release --output artifacts
```

## References

- [ALTCHA widget integration](https://altcha.org/docs/v2/widget-integration/)
- [ALTCHA server integration](https://altcha.org/it/docs/v2/server-integration/)
- [ALTCHA widget v3 / PoW compatibility](https://altcha.org/de/docs/v2/widget-v3/)
- [ALTCHA security advisory GHSA-6gvq-jcmp-8959](https://altcha.org/security-advisory/)
