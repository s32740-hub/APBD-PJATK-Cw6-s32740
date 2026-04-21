using cw7.DTO;

namespace cw7.Services;

public interface IAppointmentService
{
    Task<IEnumerable<AppointmentListDto>> GetAllAppointments();
    Task<AppointmentDetailsDto> GetAppointmentById(int idAppointment);
}