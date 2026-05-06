using Altcha.Net;
using Altcha.Net.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAltcha(options =>
{
    options.SecretKey = builder.Configuration["Altcha:SecretKey"]
        ?? Environment.GetEnvironmentVariable("ALTCHA_SECRET")
        ?? throw new InvalidOperationException("ALTCHA secret key is required.");
    options.ChallengeExpiry = TimeSpan.FromMinutes(2);
    options.Complexity = new AltchaComplexity(50000, 100000);
});

var app = builder.Build();

app.MapAltchaChallenge("/altcha/challenge");

app.MapGet("/", () => Results.Content("""
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <title>Altcha.Net Minimal API</title>
  <script async defer src="/altcha.min.js" type="module"></script>
</head>
<body>
  <form method="post" action="/contact">
    <input name="email" type="email" required>
    <textarea name="message" required></textarea>
    <altcha-widget challenge="/altcha/challenge"></altcha-widget>
    <button type="submit">Send</button>
  </form>
</body>
</html>
""", "text/html"));

app.MapPost("/contact", async (HttpRequest request, AltchaService altcha) =>
{
    var form = await request.ReadFormAsync();
    var result = altcha.ValidateResponse(form["altcha"]);

    return result.IsValid
        ? Results.Ok(new { ok = true })
        : Results.BadRequest(new { ok = false, error = result.ErrorCode });
});

app.Run();
