namespace PaymentGateway.Api.BankDtos;

public sealed record BankPaymentResult(
    bool IsOk,
    bool? Authorized,
    string? AuthorizationCode,
    bool IsUnavailable,
    bool IsBadRequest)
{
    public static BankPaymentResult Ok(bool authorized, string? code)
        => new(true, authorized, code, false, false);

    public static BankPaymentResult Unavailable()
        => new(false, null, null, true, false);

    public static BankPaymentResult BadRequest()
        => new(false, null, null, false, true);
}