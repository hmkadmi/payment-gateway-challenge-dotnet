using System.Text.Json.Serialization;

namespace PaymentGateway.Api.BankDtos;

public sealed class BankPaymentRequest
{
    [JsonPropertyName("card_number")]
    public string CardNumber { get; init; } = default!;

    // "MM/YYYY"
    [JsonPropertyName("expiry_date")]
    public string ExpiryDate { get; init; } = default!;

    [JsonPropertyName("currency")]
    public string Currency { get; init; } = default!;

    [JsonPropertyName("amount")]
    public int Amount { get; init; }

    [JsonPropertyName("cvv")]
    public string Cvv { get; init; } = default!;
}