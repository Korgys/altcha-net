# Altcha.Net

Altcha.Net est une librairie .NET open-source pour utiliser ALTCHA en mode proof-of-work auto-heberge. Elle ne depend pas d'ALTCHA Sentinel, n'appelle aucune API ALTCHA externe et vise les applications modernes comme les sites legacy ASP.NET Framework 4.8.

Le MVP implemente le format proof-of-work SHA-256 historique d'ALTCHA, compatible avec le widget ALTCHA v3 en mode legacy PoW v1.

## Cas d'usage

- Site ASP.NET Framework 4.8 legacy.
- Site VB.NET MVC ou WebForms.
- Site ASP.NET MVC C#.
- Site ASP.NET Core avec integration manuelle simple.
- Captcha leger sans serveur Sentinel.

## Installation

```bash
dotnet add package Altcha.Net
```

## Quick Start C#

```csharp
using Altcha.Net;

var store = new MemoryAltchaReplayStore();

var service = new AltchaService(new AltchaOptions
{
    SecretKey = Environment.GetEnvironmentVariable("ALTCHA_SECRET")!,
    ChallengeExpiry = TimeSpan.FromMinutes(2),
    Complexity = new AltchaComplexity(50000, 100000)
}, store);

AltchaChallenge challenge = service.GenerateChallenge();

string altchaFormValue = Request.Form["altcha"];
AltchaValidationResult result = service.ValidateResponse(altchaFormValue);

if (!result.IsValid)
{
    // Rejeter le POST et afficher une erreur utilisateur.
}
```

Pour un endpoint challenge, renvoyez directement le JSON du challenge :

```csharp
public ActionResult Challenge()
{
    return Content(AltchaProvider.Service.GenerateChallenge().ToJson(), "application/json");
}
```

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

Le widget poste ensuite un champ de formulaire `altcha` contenant un JSON encode en Base64.

## ASP.NET Framework 4.8 / VB.NET

Exemple WebForms VB.NET avec un handler `.ashx` pour le challenge.

```vbnet
Imports Altcha.Net

Public NotInheritable Class AltchaProvider
    Public Shared ReadOnly Service As New AltchaService(
        New AltchaOptions With {
            .SecretKey = Environment.GetEnvironmentVariable("ALTCHA_SECRET"),
            .ChallengeExpiry = TimeSpan.FromMinutes(2),
            .Complexity = New AltchaComplexity(50000, 100000)
        },
        New MemoryAltchaReplayStore())
End Class
```

```vbnet
Imports System.Web

Public Class AltchaChallengeHandler
    Implements IHttpHandler

    Public Sub ProcessRequest(context As HttpContext) Implements IHttpHandler.ProcessRequest
        context.Response.ContentType = "application/json"
        context.Response.Write(AltchaProvider.Service.GenerateChallenge().ToJson())
    End Sub

    Public ReadOnly Property IsReusable As Boolean Implements IHttpHandler.IsReusable
        Get
            Return True
        End Get
    End Property
End Class
```

```vbnet
Protected Sub SubmitButton_Click(sender As Object, e As EventArgs) Handles SubmitButton.Click
    Dim result = AltchaProvider.Service.ValidateResponse(Request.Form("altcha"))

    If Not result.IsValid Then
        ErrorLabel.Text = "Validation ALTCHA invalide."
        Return
    End If

    ErrorLabel.Text = ""
    SuccessLabel.Text = "Message envoye."
End Sub
```

Des fichiers complets sont disponibles dans `examples/Altcha.Net.Examples.AspNetWebForms.VbNet`.

## ASP.NET MVC C#

```csharp
public sealed class AltchaController : Controller
{
    [HttpGet]
    public ActionResult Challenge()
    {
        return Content(AltchaProvider.Service.GenerateChallenge().ToJson(), "application/json");
    }
}
```

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public ActionResult Submit(ContactForm model)
{
    var result = AltchaProvider.Service.ValidateResponse(Request.Form["altcha"]);
    if (!result.IsValid)
    {
        ModelState.AddModelError("", "Validation ALTCHA invalide.");
        return View("Index", model);
    }

    return RedirectToAction("Thanks");
}
```

Des fichiers complets sont disponibles dans `examples/Altcha.Net.Examples.AspNetMvc.CSharp`.

## ASP.NET Core

Le MVP n'ajoute pas encore de package `Altcha.Net.AspNetCore`. L'integration reste volontairement explicite :

```csharp
builder.Services.AddSingleton<IAltchaReplayStore, MemoryAltchaReplayStore>();
builder.Services.AddSingleton(sp => new AltchaService(new AltchaOptions
{
    SecretKey = builder.Configuration["Altcha:SecretKey"]!,
    ChallengeExpiry = TimeSpan.FromMinutes(2)
}, sp.GetRequiredService<IAltchaReplayStore>()));

app.MapGet("/altcha/challenge", (AltchaService altcha) => Results.Json(altcha.GenerateChallenge()));
```

## Configuration

- `SecretKey` : cle HMAC serveur. Ne jamais l'exposer au navigateur.
- `ChallengeExpiry` : duree de validite courte, par defaut 2 minutes.
- `Complexity` : plage du nombre proof-of-work. Par defaut `50000..100000`.
- `IAltchaReplayStore` : store anti-replay. Le MVP fournit `MemoryAltchaReplayStore`, thread-safe.
- `Algorithm` : seul `SHA-256` est supporte dans ce MVP.

## Production Checklist

- Stocker `SecretKey` dans un secret manager ou une variable d'environnement.
- Servir le site en HTTPS.
- Heberger le script du widget localement si votre politique de securite l'exige.
- Utiliser un store partage en multi-instance.
- Eviter `MemoryAltchaReplayStore` en production multi-serveur.
- Garder une expiration courte.
- Surveiller les erreurs sans logger les payloads ALTCHA ni la cle secrete.

## Limites du MVP

- Pas d'integration ALTCHA Sentinel.
- Pas de spam filter API.
- Pas de Redis integre.
- Proof-of-work uniquement.
- SHA-256 uniquement.
- Pas encore d'adaptateur `IDistributedCache`.
- Store memoire non recommande en multi-instance.

## Build, Test, Pack

```bash
dotnet restore Altcha.Net.sln
dotnet build Altcha.Net.sln --configuration Release
dotnet test Altcha.Net.sln --configuration Release
dotnet pack src/Altcha.Net/Altcha.Net.csproj --configuration Release --output artifacts
```

## References

- [ALTCHA widget integration](https://altcha.org/docs/v2/widget-integration/)
- [ALTCHA server integration](https://altcha.org/it/docs/v2/server-integration/)
- [ALTCHA widget v3 / PoW compatibility](https://altcha.org/de/docs/v2/widget-v3/)
- [ALTCHA security advisory GHSA-6gvq-jcmp-8959](https://altcha.org/security-advisory/)
