using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Repositoys;
using PaymentGateway.Api.Services;

using Xunit;

namespace PaymentGateway.Api.Tests;

public class PaymentsRepositoryTests
{
    [Fact]
    public void Get_WhenNotFound_ReturnsNull()
    {
        var repo = new PaymentsRepository();

        var result = repo.Get(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public void Upsert_ThenGet_ReturnsStoredPayment()
    {
        var repo = new PaymentsRepository();

        var payment = new PostPaymentResponse
        {
            Id = Guid.NewGuid(),
            Status = PaymentStatus.Authorized,
            Currency = "GBP",
            Amount = 100,
            ExpiryMonth = 4,
            ExpiryYear = 2030,
            CardNumberLastFour = 8877
        };

        repo.Upsert(payment);

        var result = repo.Get(payment.Id);

        Assert.NotNull(result);
        Assert.Equal(payment.Id, result!.Id);
        Assert.Equal(8877, result.CardNumberLastFour);
        Assert.Equal(PaymentStatus.Authorized, result.Status);
    }

    [Fact]
    public void Upsert_SameId_OverwritesExisting()
    {
        var repo = new PaymentsRepository();
        var id = Guid.NewGuid();

        repo.Upsert(new PostPaymentResponse
        {
            Id = id,
            Status = PaymentStatus.Declined,
            Currency = "GBP",
            Amount = 100,
            ExpiryMonth = 4,
            ExpiryYear = 2030,
            CardNumberLastFour = 1111
        });

        repo.Upsert(new PostPaymentResponse
        {
            Id = id,
            Status = PaymentStatus.Authorized,
            Currency = "GBP",
            Amount = 100,
            ExpiryMonth = 4,
            ExpiryYear = 2030,
            CardNumberLastFour = 2222
        });

        var result = repo.Get(id);

        Assert.NotNull(result);
        Assert.Equal(PaymentStatus.Authorized, result!.Status);
        Assert.Equal(2222, result.CardNumberLastFour);
    }
}