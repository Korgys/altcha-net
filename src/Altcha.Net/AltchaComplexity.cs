namespace Altcha.Net;

public sealed class AltchaComplexity
{
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

    public int MinNumber { get; }

    public int MaxNumber { get; }
}
