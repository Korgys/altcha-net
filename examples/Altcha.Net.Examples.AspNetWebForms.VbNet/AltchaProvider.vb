Imports Altcha.Net

Public NotInheritable Class AltchaProvider
    Private Sub New()
    End Sub

    Public Shared ReadOnly Service As New AltchaService(
        New AltchaOptions With {
            .SecretKey = Environment.GetEnvironmentVariable("ALTCHA_SECRET"),
            .ChallengeExpiry = TimeSpan.FromMinutes(2),
            .Complexity = New AltchaComplexity(50000, 100000)
        },
        New MemoryAltchaReplayStore())
End Class
