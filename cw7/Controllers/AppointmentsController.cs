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
}