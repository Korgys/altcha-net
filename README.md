# Altcha.Net

[![NuGet](https://img.shields.io/nuget/v/Altcha.Net.svg)](https://www.nuget.org/packages/Altcha.Net)
[![Build](https://github.com/Korgys/altcha-net/actions/workflows/ci.yml/badge.svg)](https://github.com/Korgys/altcha-net/actions/workflows/ci.yml)
[![License](https://img.shields.io/github/license/Korgys/altcha-net.svg)](LICENSE)

**Altcha.Net** est une librairie .NET permettant d’intégrer [ALTCHA](https://altcha.org/) en mode proof-of-work auto-hébergé.

Elle permet de protéger des formulaires publics sans dépendre d’ALTCHA Sentinel, sans appel à une API externe, sans cookie de tracking et sans service tiers de CAPTCHA.

La librairie est compatible avec les applications ASP.NET Core modernes ainsi qu’avec les applications legacy ASP.NET jusqu'au Framework 4.8.0.

## Pourquoi utiliser Altcha.Net ?

Altcha.Net répond à un besoin simple : ajouter une protection CAPTCHA légère, auto-hébergée et respectueuse de la vie privée dans une application .NET.

Cas d’usage typiques :

- formulaire de contact ;
- inscription utilisateur ;
- connexion ;
- demande de devis ;
- formulaire public exposé au spam ou aux abus automatisés.

Altcha.Net fournit :

- la génération de challenges ALTCHA côté serveur ;
- la validation des réponses côté serveur ;
- une protection anti-rejeu ;
- une intégration ASP.NET Core ;
- un usage possible dans des applications ASP.NET Framework 4.8.

Altcha.Net est une couche de protection anti-abus basée sur du proof-of-work. Ce n’est pas une solution complète d’anti-spam, de modération ou de détection avancée de bots.

## Installation

Package principal :

```bash
dotnet add package Altcha.Net
```

Intégration ASP.NET Core :

```bash
dotnet add package Altcha.Net.AspNetCore
```

## Démarrage rapide avec ASP.NET Core

Configurez Altcha.Net et exposez un endpoint de challenge :

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

var app = builder.Build();

app.MapAltchaChallenge("/altcha/challenge");

app.Run();
```

Ajoutez le widget ALTCHA dans votre formulaire :

```html
<script async defer src="/scripts/altcha.min.js" type="module"></script>

<form method="post" action="/contact">
  <input name="email" type="email" required />
  <textarea name="message" required></textarea>

  <altcha-widget challenge="/altcha/challenge"></altcha-widget>

  <button type="submit">Envoyer</button>
</form>
```

Validez ensuite le champ `altcha` côté serveur :

```csharp
app.MapPost("/contact", async (
    HttpRequest request,
    AltchaService altchaService,
    CancellationToken cancellationToken) =>
{
    var form = await request.ReadFormAsync(cancellationToken);

    var result = await altchaService.ValidateResponseAsync(
        form["altcha"],
        cancellationToken);

    if (!result.IsValid)
    {
        return Results.BadRequest(new
        {
            error = result.Error.ToString()
        });
    }

    // Traitez le formulaire ici.

    return Results.Ok();
});
```

## Configuration

Exemple `appsettings.json` :

```json
{
  "Altcha": {
    "SecretKey": "replace-with-a-secure-server-side-secret",
    "ChallengeExpiry": "00:02:00",
    "AllowedClockSkew": "00:00:10",
    "Complexity": {
      "MinNumber": 50000,
      "MaxNumber": 100000
    }
  }
}
```

Puis enregistrez la configuration :

```csharp
builder.Services.AddAltcha(builder.Configuration.GetSection("Altcha"));
```

Options principales :

| Option             | Description                                                                                      | Valeur conseillée |
| ------------------ | ------------------------------------------------------------------------------------------------ | ----------------: |
| `SecretKey`        | Clé serveur utilisée pour signer les challenges. Elle ne doit jamais être exposée au navigateur. |       Obligatoire |
| `ChallengeExpiry`  | Durée de validité d’un challenge.                                                                |       `2 minutes` |
| `AllowedClockSkew` | Tolérance aux petits écarts d’horloge entre serveurs.                                            |     `10 secondes` |
| `Complexity`       | Plage de difficulté du proof-of-work. Plus la valeur est élevée, plus le client travaille.       |   `50000..100000` |
| `SaltLength`       | Taille du sel aléatoire en octets.                                                               |              `12` |
| `MaxPayloadLength` | Taille maximale acceptée pour le payload ALTCHA.                                                 |            `4096` |
| `Algorithm`        | Algorithme utilisé pour le challenge.                                                            |         `SHA-256` |

## Protection anti-rejeu

Altcha.Net stocke les challenges déjà validés afin d’éviter leur réutilisation.

Pour une application déployée sur une seule instance, le store mémoire suffit :

```csharp
builder.Services.AddAltcha(builder.Configuration.GetSection("Altcha"));
```

Pour utiliser un cache distribué ASP.NET Core :

```csharp
builder.Services.AddDistributedMemoryCache();

builder.Services.AddAltcha(builder.Configuration.GetSection("Altcha"));
builder.Services.AddDistributedAltchaReplayStore();
```

Pour une application en production avec plusieurs instances, utilisez de préférence Redis avec une protection atomique stricte :

```csharp
using Altcha.Net.AspNetCore;
using StackExchange.Redis;

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")!));

builder.Services.AddAltcha(builder.Configuration.GetSection("Altcha"));

builder.Services.AddRedisAltchaReplayStore(sp =>
    sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());

builder.Services.AddDistributedAltchaReplayStore(
    DistributedAltchaReplayStoreMode.StrictAtomic);
```

Recommandation :

| Déploiement                                | Store recommandé                                                                                                     |
| ------------------------------------------ | -------------------------------------------------------------------------------------------------------------------- |
| Une seule instance                         | `MemoryAltchaReplayStore`                                                                                            |
| Une seule instance avec cache ASP.NET Core | `DistributedCacheAltchaReplayStore`                                                                                  |
| Plusieurs instances                        | Utilisez une solution custom avec votre couche de données habituelle avec une table pour stocker les défis et les IP |

## Sécuriser l’endpoint de challenge

L’endpoint de challenge est public. En production, il est recommandé de le protéger avec du rate limiting.

```csharp
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

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
});
```

L’en-tête `Cache-Control: no-store` est appliqué par défaut sur l’endpoint de challenge.

## Exemple avec ASP.NET Framework 4.8

Créez le service une seule fois et réutilisez-le :

```csharp
using Altcha.Net;

var service = new AltchaService(
    new AltchaOptions
    {
        SecretKey = Environment.GetEnvironmentVariable("ALTCHA_SECRET")!,
        ChallengeExpiry = TimeSpan.FromMinutes(2),
        AllowedClockSkew = TimeSpan.FromSeconds(10),
        Complexity = new AltchaComplexity(50000, 100000)
    },
    new MemoryAltchaReplayStore());
```

Endpoint MVC pour générer un challenge :

```csharp
public ActionResult Challenge()
{
    var challenge = AltchaProvider.Service.GenerateChallenge();

    return Content(challenge.ToJson(), "application/json");
}
```

Validation d’un formulaire POST :

```csharp
var result = AltchaProvider.Service.ValidateResponse(Request.Form["altcha"]);

if (!result.IsValid)
{
    ModelState.AddModelError("", "Validation ALTCHA invalide.");
    return View(model);
}
```

## Bonnes pratiques de production

Avant d’utiliser Altcha.Net en production :

- stockez `SecretKey` dans un gestionnaire de secrets ou une variable d’environnement ;
- servez le site et l’endpoint de challenge en HTTPS ;
- gardez une durée d’expiration courte ;
- appliquez du rate limiting sur l’endpoint de challenge ;
- ne loggez jamais la clé secrète ni les payloads ALTCHA complets ;
- utilisez un store anti-rejeu partagé en cas de déploiement multi-instance ;
- synchronisez les horloges serveur si plusieurs instances valident des challenges.

## Exemples

Des exemples sont disponibles dans :

- `examples/Altcha.Net.Examples.AspNetCore.MinimalApi`
- `examples/Altcha.Net.Examples.AspNetMvc.CSharp`
- `examples/Altcha.Net.Examples.AspNetWebForms.VbNet`

## Positionnement RGPD

ALTCHA est conçu comme une alternative privacy-first aux CAPTCHA traditionnels (notamment reCATCHA).

Avec Altcha.Net, les challenges sont générés et validés côté serveur dans votre propre application. Aucune API ALTCHA externe n’est nécessaire pour le fonctionnement proof-of-work de base.

Cela facilite les intégrations dans des environnements où la maîtrise des données, l’absence de tracking et la réduction des dépendances tierces sont importantes.

La conformité RGPD finale dépend toutefois de l’intégration globale de votre application.

## Projet non officiel

Altcha.Net est une implémentation communautaire non officielle.

Ce projet n’est pas affilié, approuvé ou sponsorisé par ALTCHA.
