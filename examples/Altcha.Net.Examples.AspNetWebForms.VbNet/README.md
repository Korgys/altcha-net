# Exemple ASP.NET WebForms VB.NET

Cet exemple montre le minimum a copier dans un site ASP.NET Framework 4.8 WebForms.

- `AltchaProvider.vb` garde une instance partagee du service et du store anti-replay.
- `AltchaChallengeHandler.vb` renvoie le JSON du challenge au widget.
- `Contact.aspx` contient le formulaire et le widget.
- `Contact.aspx.vb` valide le champ `Request.Form("altcha")` lors du POST.

Dans un projet WebForms reel, mappez le handler vers `/AltchaChallenge.ashx`.
