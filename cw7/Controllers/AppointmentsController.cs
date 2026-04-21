using cw7.DTO;
using cw7.Exceptions;
using cw7.Services;
using Microsoft.AspNetCore.Mvc;

namespace cw7.Controllers;
[ApiController]
[Route("api/[controller]")]
public class AppointmentsController(IAppointmentService appointmentService):ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] string? patientLastName,
    CancellationToken cancellationToken)
    {
        return Ok(await appointmentService.GetAllAppointments(status, patientLastName, cancellationToken));
    }

    [HttpGet("{idAppointment}")]
    public async Task<IActionResult> GetById([FromRoute] int idAppointment,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await appointmentService.GetAppointmentById(idAppointment, cancellationToken));
        }
        catch (NotFoundException ex)
        {
            return StatusCode(404, new ErrorResponseDto
            {
                Message = "Error Not Found",
                Details = ex.Message,
                StatusCode = 404
            });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] CreateAppointmentRequestDto appointment,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await appointmentService.CreateAppointment(appointment);
            return Ok(result);
        }
        catch (BadConditionException ex)
        {
            return StatusCode(409, new ErrorResponseDto
                {
                    Message = "Conflict",
                    Details = ex.Message,
                    StatusCode = 409
                }
            );
        }
    }

    [HttpPut("{idAppointment}")]
    public async Task<IActionResult> Update([FromRoute] int idAppointment, [FromBody] UpdateAppointmentRequestDto dto,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await appointmentService.UpdateAppointment(idAppointment, dto, cancellationToken);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return StatusCode(404, new ErrorResponseDto
                {
                    Message = "Error Not Found",
                    Details = ex.Message,
                    StatusCode = 404
                }
            );
        }
        catch (BadConditionException ex)
        {
            return StatusCode(409, new ErrorResponseDto
                {
                    Message = "Conflict",
                    Details = ex.Message,
                    StatusCode = 409
                }
            );
        }
    }
}