using System.Net;
using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using PaymentGateway.Api.Client;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _bankResponder;

    public CustomWebApplicationFactory(Func<HttpRequestMessage, HttpResponseMessage> bankResponder)
        => _bankResponder = bankResponder;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace the bank HttpClient with a fake handler
            services.RemoveAll<AcquiringBankClient>();

            services.AddHttpClient<AcquiringBankClient>()
                .ConfigurePrimaryHttpMessageHandler(() => new FakeHttpMessageHandler(_bankResponder));
        });
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
            => _responder = responder;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_responder(request));
    }

    public static HttpResponseMessage Json(HttpStatusCode statusCode, object body)
    {
        var json = JsonSerializer.Serialize(body);
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }
}