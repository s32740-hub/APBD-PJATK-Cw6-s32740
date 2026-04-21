using System.ComponentModel.DataAnnotations;

namespace cw7.DTO;

public record UpdateAppointmentRequestDto
{
    [Required]
    public int IdAppointment { get; init; }
    [Required]
    public int IdDoctor { get; init; }
    [Required]
    public int IdPatient { get; init; }

    [Required]
    public DateTime AppointmentDate { get; init; }

    [Required]
    [MaxLength(30)]
    public string Status { get; init; } = null!;

    [Required]
    [MaxLength(250)]
    public string Reason { get; init; } = null!;

    [MaxLength(500)]
    public string? InternalNotes { get; init; }
}