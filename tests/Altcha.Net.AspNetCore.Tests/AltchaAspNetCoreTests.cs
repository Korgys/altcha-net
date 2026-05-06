using System.Text.Json;
using Altcha.Net.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Altcha.Net.AspNetCore.Tests;

public sealed class AltchaAspNetCoreTests
{
    [Fact]
    public void AddAltcha_RegistersOptionsServiceAndDefaultReplayStore()
    {
        var services = new ServiceCollection();

        services.AddAltcha(options =>
        {
            options.SecretKey = "test-secret";
            options.ChallengeExpiry = TimeSpan.FromMinutes(1);
            options.Complexity = new AltchaComplexity(0, 5);
        });

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AltchaOptions>>().Value;
        var service = provider.GetRequiredService<AltchaService>();
        var replayStore = provider.GetRequiredService<IAltchaReplayStore>();

        Assert.Equal("test-secret", options.SecretKey);
        Assert.NotNull(service);
        Assert.IsType<MemoryAltchaReplayStore>(replayStore);
    }

    [Fact]
    public async Task MapAltchaChallenge_ReturnsChallengeJson()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddAltcha(options =>
        {
            options.SecretKey = "test-secret";
            options.Complexity = new AltchaComplexity(0, 5);
        });

        await using var app = builder.Build();
        app.MapAltchaChallenge("/altcha/challenge");
        await app.StartAsync();

        using var client = app.GetTestClient();

        using var response = await client.GetAsync("/altcha/challenge");
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);

        response.EnsureSuccessStatusCode();
        Assert.Equal("SHA-256", document.RootElement.GetProperty("algorithm").GetString());
        Assert.True(document.RootElement.TryGetProperty("challenge", out _));
        Assert.True(document.RootElement.TryGetProperty("salt", out _));
        Assert.True(document.RootElement.TryGetProperty("signature", out _));
        Assert.True(document.RootElement.TryGetProperty("maxnumber", out _));
    }

    [Fact]
    public void DistributedCacheReplayStore_RejectsReplayAfterFirstStore()
    {
        IDistributedCache cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var store = new DistributedCacheAltchaReplayStore(cache);

        var first = store.TryStoreOnce("same-challenge", DateTimeOffset.UtcNow.AddMinutes(1));
        var second = store.TryStoreOnce("same-challenge", DateTimeOffset.UtcNow.AddMinutes(1));

        Assert.True(first);
        Assert.False(second);
    }
}
