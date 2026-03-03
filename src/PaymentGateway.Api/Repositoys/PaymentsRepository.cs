using System.Collections.Concurrent;

using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Repositoys;

public class PaymentsRepository
{
    private readonly ConcurrentDictionary<Guid, PostPaymentResponse> _payments = new();

    public void Upsert(PostPaymentResponse payment) => _payments[payment.Id] = payment;

    public PostPaymentResponse? Get(Guid id)
        => _payments.TryGetValue(id, out var payment) ? payment : null;
}