namespace cw7.DTO;
public record AppointmentDetailsDto
{
    public string PatientName { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string PhoneNumber { get; init; } = null!;
    public string DoctorName { get; init; } = null!;
    public string LicenseNumber { get; init; } = null!;
    public DateTime AppointmentDate { get; init; }
    public string? InternalNotes { get; init; }
}