namespace Altcha.Net;

/// <summary>
/// Represents the outcome of validating an ALTCHA response.
/// </summary>
public sealed class AltchaValidationResult
{
    private AltchaValidationResult(bool isValid, AltchaValidationError error)
    {
        IsValid = isValid;
        Error = error;
    }

    /// <summary>
    /// Gets whether the ALTCHA response was accepted.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Gets the validation error when <see cref="IsValid"/> is <c>false</c>.
    /// </summary>
    public AltchaValidationError Error { get; }

    /// <summary>
    /// Gets the validation error as a stable string for logs or API responses.
    /// </summary>
    public string ErrorCode => Error.ToString();

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <returns>A valid ALTCHA validation result.</returns>
    public static AltchaValidationResult Success()
    {
        return new AltchaValidationResult(true, AltchaValidationError.None);
    }

    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    /// <param name="error">The reason validation failed.</param>
    /// <returns>An invalid ALTCHA validation result.</returns>
    public static AltchaValidationResult Failure(AltchaValidationError error)
    {
        return new AltchaValidationResult(false, error);
    }
}
