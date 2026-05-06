# Exemple ASP.NET MVC C#

Cet exemple montre le minimum a copier dans une application ASP.NET MVC sur .NET Framework 4.8.

- `AltchaProvider.cs` garde une instance partagee du service et du store anti-replay.
- `AltchaController.cs` expose `/Altcha/Challenge`.
- `ContactController.cs` valide `Request.Form["altcha"]` dans le POST.
- `Views/Contact/Index.cshtml` contient le widget.

## Etapes

1. Installer le package `Altcha.Net` dans le projet MVC.
2. Definir `ALTCHA_SECRET` dans l'environnement du serveur.
3. Copier les fichiers d'exemple dans l'application.
4. Servir le script `altcha.min.js` depuis les assets du site.
5. Verifier que `/Altcha/Challenge` renvoie un JSON de challenge.

`MemoryAltchaReplayStore` convient pour un exemple local ou une seule instance. Utiliser un store partage en production multi-instance.
