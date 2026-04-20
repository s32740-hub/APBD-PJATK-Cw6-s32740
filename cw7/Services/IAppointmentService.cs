using cw7.DTO;

namespace cw7.Services;

public interface IAppointmentService
{
    Task<IEnumerable<AppointmentDetailsDto>> GetAllAppointments();
}