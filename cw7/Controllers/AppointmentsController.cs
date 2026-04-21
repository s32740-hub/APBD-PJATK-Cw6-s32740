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
    public async Task<IActionResult> GetAll()
    {
        return Ok(await appointmentService.GetAllAppointments());
    }

    [HttpGet("{idAppointment}")]
    public async Task<IActionResult> GetById([FromRoute] int idAppointment)
    {
        try
        {
            return Ok(await appointmentService.GetAppointmentById(idAppointment));
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
}