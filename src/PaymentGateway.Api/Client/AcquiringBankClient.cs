using System.Net;
using System.Net.Http.Json;

using PaymentGateway.Api.BankDtos;

namespace PaymentGateway.Api.Client;

public sealed class AcquiringBankClient
{
    private readonly HttpClient _http;

    public AcquiringBankClient(HttpClient http) => _http = http;

    public async Task<BankPaymentResult> PayAsync(BankPaymentRequest request, CancellationToken ct)
    {
        using var response = await _http.PostAsJsonAsync("/payments", request, ct);

        if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
            return BankPaymentResult.Unavailable();

        if (response.StatusCode == HttpStatusCode.BadRequest)
            return BankPaymentResult.BadRequest();

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<BankPaymentResponse>(cancellationToken: ct)
                   ?? throw new InvalidOperationException("Bank response was empty.");

        return BankPaymentResult.Ok(body.Authorized, body.AuthorizationCode);
    }
}