using cw7.DTO;
using Microsoft.Data.SqlClient;

namespace cw7.Services;

public class AppointmentService(IConfiguration configuration):IAppointmentService
{
    public async Task<IEnumerable<AppointmentDetailsDto>> GetAllAppointments()
    {
        List<AppointmentDetailsDto> result = [];
        await using var connection = new SqlConnection(configuration.GetConnectionString("Default"));        
        await using var command = new SqlCommand();
        command.Connection = connection;
        command.CommandText = """
                              SELECT 
                                  a.IdAppointment, a.AppointmentDate, a.IdDoctor, a.Status, a.Reason, a.InternalNotes, a.CreatedAt,
                                  p.FirstName AS PatientFirstName, p.LastName AS PatientLastName
                              FROM Appointments a
                              JOIN Patients p ON a.IdPatient = p.IdPatient
                              JOIN Doctors d ON a.IdDoctor = d.IdDoctor
                              """;
        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(
                new AppointmentDetailsDto
                {
                    IdAppointment = (int)reader["IdAppointment"],
                    AppointmentDate = (DateTime)reader["AppointmentDate"],
                    Status = reader["Status"] as string,
                    Reason = reader["Reason"] as string,
                    InternalNotes = reader["InternalNotes"] as string,
                    CreatedAt = (DateTime)reader["CreatedAt"],
                    IdDoctor =  (int)reader["IdDoctor"],
            
                    PatientName = $"{reader["PatientFirstName"]} {reader["PatientLastName"]}",
                }
            );
        }
        return result;
    }
}