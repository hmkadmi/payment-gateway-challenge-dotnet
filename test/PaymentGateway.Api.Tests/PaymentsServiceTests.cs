using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

using PaymentGateway.Api.Client;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Repositoys;
using PaymentGateway.Api.Services;

using Xunit;

namespace PaymentGateway.Api.Tests;

public class PaymentsServiceTests
{
    [Fact]
    public async Task ProcessAsync_WhenBankAuthorizes_ReturnsAuthorized_AndStoresPayment()
    {
        var repo = new PaymentsRepository();
        var bank = CreateBankClient(HttpStatusCode.OK, new { authorized = true, authorization_code = "auth-123" });
        var service = new PaymentsService(repo, bank);

        var req = ValidRequest("2222405343248877"); // last4 8877

        var (payment, error) = await service.ProcessAsync(req, CancellationToken.None);

        Assert.Null(error);
        Assert.NotNull(payment);
        Assert.Equal(PaymentStatus.Authorized, payment!.Status);
        Assert.Equal(8877, payment.CardNumberLastFour);

        var stored = repo.Get(payment.Id);
        Assert.NotNull(stored);
        Assert.Equal(payment.Id, stored!.Id);
    }

    [Fact]
    public async Task ProcessAsync_WhenBankDeclines_ReturnsDeclined()
    {
        var repo = new PaymentsRepository();
        var bank = CreateBankClient(HttpStatusCode.OK, new { authorized = false, authorization_code = (string?)null });
        var service = new PaymentsService(repo, bank);

        var req = ValidRequest("2222405343248878");

        var (payment, error) = await service.ProcessAsync(req, CancellationToken.None);

        Assert.Null(error);
        Assert.NotNull(payment);
        Assert.Equal(PaymentStatus.Declined, payment!.Status);
    }

    [Fact]
    public async Task ProcessAsync_WhenBankUnavailable_ReturnsBankUnavailableError_AndDoesNotStore()
    {
        var repo = new PaymentsRepository();
        var bank = CreateBankClient(HttpStatusCode.ServiceUnavailable, body: null);
        var service = new PaymentsService(repo, bank);

        var req = ValidRequest("2222405343248870");

        var (payment, error) = await service.ProcessAsync(req, CancellationToken.None);

        Assert.Null(payment);
        Assert.Equal(PaymentServiceError.BankUnavailable, error);

        // nothing stored
        Assert.Null(repo.Get(Guid.NewGuid()));
    }

    [Fact]
    public async Task ProcessAsync_MapsLast4Correctly()
    {
        var repo = new PaymentsRepository();
        var bank = CreateBankClient(HttpStatusCode.OK, new { authorized = true, authorization_code = "x" });
        var service = new PaymentsService(repo, bank);

        var req = ValidRequest("1234567890123456"); // last4 3456

        var (payment, error) = await service.ProcessAsync(req, CancellationToken.None);

        Assert.Null(error);
        Assert.NotNull(payment);
        Assert.Equal(3456, payment!.CardNumberLastFour);
    }

    private static PostPaymentRequest ValidRequest(string pan) => new()
    {
        CardNumber = pan,
        ExpiryMonth = 4,
        ExpiryYear = 2030,
        Currency = "GBP",
        Amount = 100,
        Cvv = "123"
    };

    /// <summary>
    /// Builds an AcquiringBankClient that returns the given HTTP response without hitting the network.
    /// </summary>
    private static AcquiringBankClient CreateBankClient(HttpStatusCode status, object? body)
    {
        var handler = new FakeHandler(_ =>
        {
            var response = new HttpResponseMessage(status);

            if (body is not null)
            {
                var json = JsonSerializer.Serialize(body);
                response.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            return response;
        });

        var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:8080")
        };

        return new AcquiringBankClient(http);
    }

    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
            => _responder = responder;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_responder(request));
    }
}