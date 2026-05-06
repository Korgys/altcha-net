# Exemple ASP.NET Core Minimal API

Cet exemple montre l'integration optionnelle `Altcha.Net.AspNetCore`.

## Utilisation

```bash
set ALTCHA_SECRET=change-me-in-development
dotnet run --project examples/Altcha.Net.Examples.AspNetCore.MinimalApi
```

Expose:

- `GET /altcha/challenge` pour le widget ALTCHA
- `POST /contact` pour valider le champ formulaire `altcha`

Le fichier HTML suppose que le script du widget est servi localement sous `/altcha.min.js`. Dans une vraie application, servez le script depuis vos assets statiques ou depuis une source approuvee.
