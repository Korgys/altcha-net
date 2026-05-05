# Exemple ASP.NET MVC C#

Cet exemple montre le minimum a copier dans une application ASP.NET MVC sur .NET Framework 4.8.

- `AltchaProvider.cs` garde une instance partagee du service et du store anti-replay.
- `AltchaController.cs` expose `/Altcha/Challenge`.
- `ContactController.cs` valide `Request.Form["altcha"]` dans le POST.
- `Views/Contact/Index.cshtml` contient le widget.
