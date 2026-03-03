using System.Text.Json.Serialization;
using System.Text.Json;

using PaymentGateway.Api.Client;
using PaymentGateway.Api.Repositoys;
using PaymentGateway.Api.Services;

var builder = WebApplication.CreateBuilder(args);


builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "PaymentGateway.Api", Version = "v1" });

    // Ensure Swagger uses JSON
    c.MapType<PaymentGateway.Api.Models.PaymentStatus>(() =>
        new Microsoft.OpenApi.Models.OpenApiSchema { Type = "string" });
});
// Storage
builder.Services.AddSingleton<PaymentsRepository>();

// Bank client (simulator)
builder.Services.AddHttpClient<AcquiringBankClient>(client =>
{
    client.BaseAddress = new Uri("http://localhost:8080");
});

// Domain service
builder.Services.AddScoped<PaymentsService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();
app.Run();
public partial class Program { }