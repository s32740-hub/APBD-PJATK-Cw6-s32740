namespace cw7.DTO;

public class ErrorResponseDto
{
    public string Message { get; init; } = null!;
    public string? Details { get; init; }
    public int StatusCode { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}