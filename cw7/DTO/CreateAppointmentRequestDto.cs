using System.ComponentModel.DataAnnotations;

namespace cw7.DTO;

public class CreateAppointmentRequestDto
{
    [Required]
    public int IdPatient { get; init; }

    [Required]
    public int IdDoctor { get; init; }

    [Required]
    public DateTime AppointmentDate { get; init; }
    
    [Required]
    [MaxLength(250)]
    public string Reason { get; init; } = null!;

    [MaxLength(500)]
    public string? InternalNotes { get; init; }
}