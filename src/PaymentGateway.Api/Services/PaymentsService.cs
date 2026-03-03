using PaymentGateway.Api.BankDtos;
using PaymentGateway.Api.Client;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Repositoys;

namespace PaymentGateway.Api.Services;

public sealed class PaymentsService
{
    private readonly PaymentsRepository _repo;
    private readonly AcquiringBankClient _bank;

    public PaymentsService(PaymentsRepository repo, AcquiringBankClient bank)
    {
        _repo = repo;
        _bank = bank;
    }

    public async Task<(PostPaymentResponse? payment, PaymentServiceError? error)> ProcessAsync(
        PostPaymentRequest req,
        CancellationToken ct)
    {
        // At this point ModelState validation already happened in controller.
        // Map to bank request
        var bankReq = new BankPaymentRequest
        {
            CardNumber = req.CardNumber,
            ExpiryDate = $"{req.ExpiryMonth:00}/{req.ExpiryYear}",
            Currency = req.Currency.ToUpperInvariant(),
            Amount = req.Amount,
            Cvv = req.Cvv
        };

        var bankRes = await _bank.PayAsync(bankReq, ct);

        if (bankRes.IsUnavailable)
            return (null, PaymentServiceError.BankUnavailable);

        if (bankRes.IsBadRequest)
            // Bank says request malformed: in practice our validation should prevent it,
            // but we handle it defensively.
            return (null, PaymentServiceError.BankRejectedRequest);

        var status = bankRes.Authorized == true ? PaymentStatus.Authorized : PaymentStatus.Declined;

        var payment = new PostPaymentResponse
        {
            Id = Guid.NewGuid(),
            Status = status,
            CardNumberLastFour = int.Parse(req.CardNumber[^4..]),
            ExpiryMonth = req.ExpiryMonth,
            ExpiryYear = req.ExpiryYear,
            Currency = req.Currency.ToUpperInvariant(),
            Amount = req.Amount
        };

        _repo.Upsert(payment);
        return (payment, null);
    }
}

public enum PaymentServiceError
{
    BankUnavailable,
    BankRejectedRequest
}