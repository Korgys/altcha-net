using System.Text.Json;
using Altcha.Net.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

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

    [Fact]
    public async Task DistributedCacheReplayStore_BestEffort_AllowsRaceAcrossWorkers()
    {
        var backend = new ConcurrentDictionary<string, (string Value, DateTimeOffset ExpiresAt)>();
        var worker1 = new DistributedCacheAltchaReplayStore(new RaceyDistributedCache(backend));
        var worker2 = new DistributedCacheAltchaReplayStore(new RaceyDistributedCache(backend));
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(1);

        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var t1 = Task.Run(async () =>
        {
            await gate.Task;
            return worker1.TryStoreOnce("shared-challenge", expiresAt);
        });
        var t2 = Task.Run(async () =>
        {
            await gate.Task;
            return worker2.TryStoreOnce("shared-challenge", expiresAt);
        });

        gate.SetResult();
        var results = await Task.WhenAll(t1, t2);

        Assert.Equal(2, results.Count(v => v));
    }

    [Fact]
    public async Task DistributedCacheReplayStore_StrictAtomic_PreventsRaceAcrossWorkers()
    {
        var backend = new ConcurrentDictionary<string, DateTimeOffset>();
        var atomic = new InMemoryAtomicAltchaReplayStore(backend);
        var worker1 = new DistributedCacheAltchaReplayStore(
            new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())), atomic);
        var worker2 = new DistributedCacheAltchaReplayStore(
            new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())), atomic);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(1);

        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var t1 = Task.Run(async () =>
        {
            await gate.Task;
            return worker1.TryStoreOnce("shared-challenge", expiresAt);
        });
        var t2 = Task.Run(async () =>
        {
            await gate.Task;
            return worker2.TryStoreOnce("shared-challenge", expiresAt);
        });

        gate.SetResult();
        var results = await Task.WhenAll(t1, t2);

        Assert.Equal(1, results.Count(v => v));
    }

    private sealed class InMemoryAtomicAltchaReplayStore(ConcurrentDictionary<string, DateTimeOffset> backend)
        : IAtomicAltchaReplayStore
    {
        public bool TryStoreOnceAtomic(string key, DateTimeOffset expiresAt)
            => backend.TryAdd(key, expiresAt);
    }

    private sealed class RaceyDistributedCache(ConcurrentDictionary<string, (string Value, DateTimeOffset ExpiresAt)> backend) : IDistributedCache
    {
        public byte[]? Get(string key)
        {
            if (backend.TryGetValue(key, out var entry) && entry.ExpiresAt > DateTimeOffset.UtcNow)
            {
                return System.Text.Encoding.UTF8.GetBytes(entry.Value);
            }

            return null;
        }

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => Task.FromResult(Get(key));
        public void Refresh(string key) { }
        public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;
        public void Remove(string key) => backend.TryRemove(key, out _);
        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            Remove(key);
            return Task.CompletedTask;
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            var expiresAt = options.AbsoluteExpiration ?? DateTimeOffset.UtcNow.AddMinutes(1);
            Thread.Sleep(20);
            backend[key] = (System.Text.Encoding.UTF8.GetString(value), expiresAt);
        }

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            Set(key, value, options);
            return Task.CompletedTask;
        }
    }
}
