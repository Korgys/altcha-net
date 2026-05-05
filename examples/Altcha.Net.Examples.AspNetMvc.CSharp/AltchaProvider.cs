using System;
using Altcha.Net;

namespace Altcha.Net.Examples.AspNetMvc.CSharp
{
    public static class AltchaProvider
    {
        public static readonly AltchaService Service = new AltchaService(
            new AltchaOptions
            {
                SecretKey = Environment.GetEnvironmentVariable("ALTCHA_SECRET"),
                ChallengeExpiry = TimeSpan.FromMinutes(2),
                Complexity = new AltchaComplexity(50000, 100000)
            },
            new MemoryAltchaReplayStore());
    }
}
