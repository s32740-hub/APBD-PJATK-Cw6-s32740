using cw7.DTO;

namespace cw7.Services;

public interface IAppointmentService
{
    Task<IEnumerable<AppointmentListDto>> GetAllAppointments(string? status, string? patientLastName, CancellationToken cancellationToken);
    Task<AppointmentDetailsDto> GetAppointmentById(int idAppointment, CancellationToken cancellationToken);
    Task<AppointmentDetailsDto> CreateAppointment(CreateAppointmentRequestDto appointment, CancellationToken cancellationToken = default);
    Task RemoveAppointment(int idAppointment, CancellationToken cancellationToken = default);

    Task UpdateAppointment(int id, UpdateAppointmentRequestDto appointment,
        CancellationToken cancellationToken = default);
}