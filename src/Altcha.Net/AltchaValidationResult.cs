namespace Altcha.Net;

public sealed class AltchaValidationResult
{
    private AltchaValidationResult(bool isValid, AltchaValidationError error)
    {
        IsValid = isValid;
        Error = error;
    }

    public bool IsValid { get; }

    public AltchaValidationError Error { get; }

    public string ErrorCode => Error.ToString();

    public static AltchaValidationResult Success()
    {
        return new AltchaValidationResult(true, AltchaValidationError.None);
    }

    public static AltchaValidationResult Failure(AltchaValidationError error)
    {
        return new AltchaValidationResult(false, error);
    }
}
