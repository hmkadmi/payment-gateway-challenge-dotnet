using System.ComponentModel.DataAnnotations;

namespace PaymentGateway.Api.Models.Requests;

public sealed class PostPaymentRequest : IValidatableObject
{
    [Required]
    [RegularExpression(@"^\d{14,19}$", ErrorMessage = "Card number must be 14-19 digits.")]
    public string CardNumber { get; init; } = default!;

    [Required]
    [Range(1, 12)]
    public int ExpiryMonth { get; init; }

    [Required]
    [Range(2000, 2100)]
    public int ExpiryYear { get; init; }

    [Required]
    [RegularExpression(@"^[A-Z]{3}$", ErrorMessage = "Currency must be 3 uppercase letters.")]
    public string Currency { get; init; } = default!;

    [Required]
    [Range(1, int.MaxValue)]
    public int Amount { get; init; }

    [Required]
    [RegularExpression(@"^\d{3,4}$", ErrorMessage = "CVV must be 3-4 digits.")]
    public string Cvv { get; init; } = default!;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Only allow up to 3 currencies (requirement)
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "GBP", "USD", "EUR" };
        if (!allowed.Contains(Currency))
            yield return new ValidationResult("Unsupported currency.", new[] { nameof(Currency) });

        // Expiry must be in the future (month+year)
        var now = DateTimeOffset.UtcNow;
        var endOfExpiryMonth = new DateTimeOffset(ExpiryYear, ExpiryMonth, 1, 0, 0, 0, TimeSpan.Zero)
            .AddMonths(1)
            .AddTicks(-1);

        if (endOfExpiryMonth <= now)
            yield return new ValidationResult("Card is expired.", new[] { nameof(ExpiryMonth), nameof(ExpiryYear) });
    }
}