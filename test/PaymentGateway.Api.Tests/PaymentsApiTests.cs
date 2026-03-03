using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using FluentAssertions;

using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Responses;

using Xunit;

namespace PaymentGateway.Api.Tests;

public class PaymentsApiTests
{
    [Fact]
    public async Task PostPayment_WhenBankAuthorizes_Returns201AndAuthorized()
    {
        // Arrange: bank returns authorized
        using var factory = new CustomWebApplicationFactory(_ =>
            CustomWebApplicationFactory.Json(HttpStatusCode.OK, new
            {
                authorized = true,
                authorization_code = "auth-123"
            }));

        var client = factory.CreateClient();

        var req = new
        {
            cardNumber = "2222405343248877", // odd => authorized in simulator, but here bank is mocked
            expiryMonth = 4,
            expiryYear = 2030,
            currency = "GBP",
            amount = 100,
            cvv = "123"
        };

        // Act
        var res = await client.PostAsJsonAsync("/api/payments", req);

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await res.Content.ReadFromJsonAsync<PostPaymentResponse>(
    new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    }); ;
        body.Should().NotBeNull();
        body!.Status.Should().Be(PaymentStatus.Authorized);
        body.Currency.Should().Be("GBP");
        body.Amount.Should().Be(100);
        body.CardNumberLastFour.Should().Be(8877);
    }

    [Fact]
    public async Task PostPayment_WhenBankDeclines_Returns201AndDeclined()
    {
        using var factory = new CustomWebApplicationFactory(_ =>
            CustomWebApplicationFactory.Json(HttpStatusCode.OK, new
            {
                authorized = false,
                authorization_code = (string?)null
            }));

        var client = factory.CreateClient();

        var req = new
        {
            cardNumber = "2222405343248878",
            expiryMonth = 4,
            expiryYear = 2030,
            currency = "GBP",
            amount = 100,
            cvv = "123"
        };

        var res = await client.PostAsJsonAsync("/api/payments", req);

        res.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await res.Content.ReadFromJsonAsync<PostPaymentResponse>(
    new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    }); ;
        body!.Status.Should().Be(PaymentStatus.Declined);
    }

    [Fact]
    public async Task PostPayment_WhenBankUnavailable_Returns503()
    {
        using var factory = new CustomWebApplicationFactory(_ =>
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        var client = factory.CreateClient();

        var req = new
        {
            cardNumber = "2222405343248870",
            expiryMonth = 4,
            expiryYear = 2030,
            currency = "GBP",
            amount = 100,
            cvv = "123"
        };

        var res = await client.PostAsJsonAsync("/api/payments", req);

        res.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task PostPayment_WhenInvalid_Returns400()
    {
        // bank should not be called; we still provide a responder
        using var factory = new CustomWebApplicationFactory(_ =>
            throw new Exception("Bank should not be called for invalid requests"));

        var client = factory.CreateClient();

        var invalidReq = new
        {
            cardNumber = "123", // invalid
            expiryMonth = 1,
            expiryYear = 2000, // expired
            currency = "XXX",  // unsupported
            amount = 0,        // invalid
            cvv = "1"          // invalid
        };

        var res = await client.PostAsJsonAsync("/api/payments", invalidReq);

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPayment_AfterPost_ReturnsStoredPayment()
    {
        using var factory = new CustomWebApplicationFactory(_ =>
            CustomWebApplicationFactory.Json(HttpStatusCode.OK, new { authorized = true, authorization_code = "auth-999" }));

        var client = factory.CreateClient();

        var req = new
        {
            cardNumber = "2222405343248877",
            expiryMonth = 4,
            expiryYear = 2030,
            currency = "GBP",
            amount = 100,
            cvv = "123"
        };

        var post = await client.PostAsJsonAsync("/api/payments", req);
        post.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await post.Content.ReadFromJsonAsync<PostPaymentResponse>(
    new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    }); ;



        created.Should().NotBeNull();

        var get = await client.GetAsync($"/api/payments/{created!.Id}");
        get.StatusCode.Should().Be(HttpStatusCode.OK);

        var fetched = await get.Content.ReadFromJsonAsync<PostPaymentResponse>(
    new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    }); ;
        fetched!.Id.Should().Be(created.Id);
        fetched.CardNumberLastFour.Should().Be(8877);
        fetched.Status.Should().Be(PaymentStatus.Authorized);
    }

    [Fact]
    public async Task Returns404IfPaymentNotFound()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory(_ =>
            CustomWebApplicationFactory.Json(HttpStatusCode.OK, new { authorized = true, authorization_code = "x" }));

        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/Payments/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}