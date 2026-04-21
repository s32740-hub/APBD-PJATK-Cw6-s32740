namespace cw7.DTO;
public class AppointmentListDto
{
    public int IdAppointment { get; init; }
    public string PatientName { get; init; }
    public int IdDoctor { get; init; }
    public DateTime AppointmentDate { get; init; }
    public string Status { get; init; } = null!;
    public string Reason { get; init; } = null!;
    public string? InternalNotes { get; init; }
    public DateTime CreatedAt { get; init; }
}