using cw7.DTO;
using cw7.Exceptions;
using Microsoft.Data.SqlClient;

namespace cw7.Services;

public class AppointmentService(IConfiguration configuration):IAppointmentService
{
    public async Task<IEnumerable<AppointmentListDto>> GetAllAppointments()
    {
        List<AppointmentListDto> result = [];
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
                new AppointmentListDto
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

    public async Task<AppointmentDetailsDto> GetAppointmentById(int idAppointment)
    {
        AppointmentDetailsDto? result = null;
        await using var connection = new SqlConnection(configuration.GetConnectionString("Default"));        
        await using var command = new SqlCommand();
        command.Connection = connection;
        command.CommandText = """
                              SELECT p.FirstName + ' ' + p.LastName as PatientName,
                                                                   p.Email, p.PhoneNumber,
                                                                   d.FirstName + ' ' + d.LastName as DoctorName,
                                                                   d.LicenseNumber,
                                                                   AppointmentDate, InternalNotes
                              FROM Appointments a
                                  Join Patients p ON a.IdPatient = p.IdPatient
                                  join  Doctors d ON a.IdDoctor = d.IdDoctor
                              where a.IdAppointment = @idAppointment
                              """;
        command.Parameters.AddWithValue("@idAppointment", idAppointment);
        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (result is null)
            {
                result = (
                    new AppointmentDetailsDto
                    {
                        PatientName = reader.GetString(0),
                        Email = reader.GetString(1),
                        PhoneNumber = reader.GetString(2),
                        DoctorName = reader.GetString(3),
                        LicenseNumber = reader.GetString(4),
                        AppointmentDate = reader.GetDateTime(5),
                        InternalNotes = await reader.IsDBNullAsync(6) ? null : reader.GetString(6)
                    }
                );
            }
        }

        if (result is null)
        {
            throw new NotFoundException($"Appointment with id {idAppointment} not found");
        }
        return result;
    }
}