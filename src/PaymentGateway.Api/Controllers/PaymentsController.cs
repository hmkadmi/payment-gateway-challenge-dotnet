using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Repositoys;
using PaymentGateway.Api.Services;
using Microsoft.AspNetCore.Http;
using PaymentGateway.Api.Models.Requests;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : ControllerBase
{
    private readonly PaymentsRepository _paymentsRepository;
    private readonly PaymentsService _paymentsService;

    public PaymentsController(PaymentsRepository paymentsRepository, PaymentsService paymentsService)
    {
        _paymentsRepository = paymentsRepository;
        _paymentsService = paymentsService;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PostPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<PostPaymentResponse> GetPayment(Guid id)
    {
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
        if (!ModelState.IsValid)
        {
            // Requirement: invalid request => Rejected and do not call bank.
            // Here we return 400; status "Rejected" is a domain concept, not a persisted payment.
            return ValidationProblem(ModelState);
        }

        var (payment, error) = await _paymentsService.ProcessAsync(request, ct);

        if (error == PaymentServiceError.BankUnavailable)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Bank is unavailable." });

        if (error == PaymentServiceError.BankRejectedRequest)
            return StatusCode(StatusCodes.Status502BadGateway, new { message = "Bank rejected the request." });

        return CreatedAtAction(nameof(GetPayment), new { id = payment!.Id }, payment);
    }
}