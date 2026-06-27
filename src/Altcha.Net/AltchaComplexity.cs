namespace Altcha.Net;

/// <summary>
/// Defines the numeric proof-of-work range used when creating ALTCHA challenges.
/// </summary>
public sealed class AltchaComplexity
{
    /// <summary>
    /// Creates a proof-of-work range.
    /// </summary>
    /// <param name="minNumber">The inclusive lower bound for generated numbers.</param>
    /// <param name="maxNumber">The inclusive upper bound clients may need to search.</param>
    public AltchaComplexity(int minNumber, int maxNumber)
    {
        if (minNumber < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minNumber), "The minimum number must be greater than or equal to zero.");
        }

        if (maxNumber < minNumber)
        {
            throw new ArgumentOutOfRangeException(nameof(maxNumber), "The maximum number must be greater than or equal to the minimum number.");
        }

        MinNumber = minNumber;
        MaxNumber = maxNumber;
    }

    /// <summary>
    /// Gets the inclusive lower bound for generated numbers.
    /// </summary>
    public int MinNumber { get; }

    /// <summary>
    /// Gets the inclusive upper bound clients may need to search.
    /// </summary>
    public int MaxNumber { get; }
}
