# Exemple ASP.NET WebForms VB.NET

Cet exemple montre le minimum a copier dans un site ASP.NET Framework 4.8 WebForms.

- `AltchaProvider.vb` garde une instance partagee du service et du store anti-replay.
- `AltchaChallengeHandler.vb` renvoie le JSON du challenge au widget.
- `Contact.aspx` contient le formulaire et le widget.
- `Contact.aspx.vb` valide le champ `Request.Form("altcha")` lors du POST.

Dans un projet WebForms reel, mappez le handler vers `/AltchaChallenge.ashx`.

## Etapes

1. Installer le package `Altcha.Net` dans le projet WebForms.
2. Definir `ALTCHA_SECRET` dans l'environnement du serveur.
3. Ajouter le handler `.ashx` ou le mapper vers une route equivalente.
4. Servir le script `altcha.min.js` depuis les assets du site.
5. Verifier que le handler renvoie un JSON de challenge.

`MemoryAltchaReplayStore` convient pour un exemple local ou une seule instance. Utiliser un store partage en production multi-instance.
