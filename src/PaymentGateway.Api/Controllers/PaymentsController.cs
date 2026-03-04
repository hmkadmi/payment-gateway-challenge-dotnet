using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Repositoys;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : ControllerBase
{
    private readonly PaymentsRepository _paymentsRepository;
    private readonly PaymentsService _paymentsService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(PaymentsRepository paymentsRepository, PaymentsService paymentsService, ILogger<PaymentsController> logger)
    {
        _paymentsRepository = paymentsRepository;
        _paymentsService = paymentsService;
        _logger = logger;

    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PostPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<PostPaymentResponse> GetPayment(Guid id)
    {
        _logger.LogInformation("GET payment requested. PaymentId={PaymentId}", id);
        var payment = _paymentsRepository.Get(id);
        return payment is null ? NotFound() : Ok(payment);
    }

    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(PostPaymentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<PostPaymentResponse>> PostPayment([FromBody] PostPaymentRequest request, CancellationToken ct)
    {
        _logger.LogInformation("POST payment received. Currency={Currency} Amount={Amount} Expiry={ExpiryMonth}/{ExpiryYear} Last4={Last4}",
    request.Currency, request.Amount, request.ExpiryMonth, request.ExpiryYear,
    request.CardNumber?.Length >= 4 ? request.CardNumber[^4..] : "N/A");

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Payment rejected due to validation errors.");
            // Requirement: invalid request => Rejected and do not call bank.
            // Here we return 400; status "Rejected" is a domain concept, not a persisted payment.
            return ValidationProblem(ModelState);
        }

        var (payment, error) = await _paymentsService.ProcessAsync(request, ct);

        if (error == PaymentServiceError.BankUnavailable)
        {
            _logger.LogError("Bank unavailable while processing payment.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Bank is unavailable." });
        }

        if (error == PaymentServiceError.BankRejectedRequest)
            return StatusCode(StatusCodes.Status502BadGateway, new { message = "Bank rejected the request." });

        _logger.LogInformation("Payment processed. PaymentId={PaymentId} Status={Status}", payment!.Id, payment.Status);
        return CreatedAtAction(nameof(GetPayment), new { id = payment!.Id }, payment);
    }
}