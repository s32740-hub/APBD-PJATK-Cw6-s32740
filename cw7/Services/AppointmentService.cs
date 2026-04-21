using System.Text;
using cw7.DTO;
using cw7.Exceptions;
using Microsoft.Data.SqlClient;

namespace cw7.Services;

public class AppointmentService(IConfiguration configuration):IAppointmentService
{
    public async Task<IEnumerable<AppointmentListDto>> GetAllAppointments(string? status, string? patientLastName, CancellationToken cancellationToken)
    {
        var result = new List <AppointmentListDto>();
        var sqlCommand = new StringBuilder("""
                                           SELECT 
                                               a.IdAppointment, a.AppointmentDate, a.IdDoctor, a.Status, a.Reason, a.InternalNotes, a.CreatedAt,
                                               p.FirstName AS PatientFirstName, p.LastName AS PatientLastName
                                           FROM Appointments a
                                           JOIN Patients p ON a.IdPatient = p.IdPatient
                                           JOIN Doctors d ON a.IdDoctor = d.IdDoctor
                                           """);
        var conditions = new List<string>();
        var parameters = new List<SqlParameter>();
        if (status != null)
        {
            conditions.Add("Status = @status");
            parameters.Add(new SqlParameter("@status", status));
        }

        if (patientLastName != null)
        {
            conditions.Add("p.LastName = @patientLastName");
            parameters.Add(new SqlParameter("@patientLastName", patientLastName));
        }

        if (parameters.Count > 0)
        {
            sqlCommand.Append(" WHERE ");
            sqlCommand.Append(string.Join(" AND ", conditions));
        }
        await using var connection = new SqlConnection(configuration.GetConnectionString("Default"));
        await using var command = new SqlCommand();
        command.Connection = connection;
        command.CommandText = sqlCommand.ToString();
        command.Parameters.AddRange(parameters.ToArray());
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
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

    public async Task<AppointmentDetailsDto> GetAppointmentById(int idAppointment, CancellationToken cancellationToken)
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
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (result is null)
            {
                result ??= (
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

    public async Task<AppointmentDetailsDto> CreateAppointment(CreateAppointmentRequestDto appointment, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(configuration.GetConnectionString("Default"));        
        await using var command = new SqlCommand();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        command.Connection = connection;
        command.Transaction = (SqlTransaction)transaction;
        command.CommandText = """
                              SELECT 
                                (SELECT COUNT(*) FROM Doctors 
                                                 WHERE IdDoctor = @idDoctor 
                                                   And IsActive = 1
                                                 And NOT EXISTS (
                                                         SELECT 1 From Appointments
                                                            Where IdDoctor = @idDoctor AND AppointmentDate = @Date
                                                         )
                                ) AS DoctorExists,
                                (SELECT COUNT(*) FROM Patients WHERE IdPatient = @idPatient And IsActive = 1) AS PatientExists,
                                (SELECT CASE WHEN @Date > GETDATE() THEN 1 ELSE 0 END) AS DateCondition
                              """;
        command.Parameters.AddWithValue("@Notes", (object?)appointment.InternalNotes ?? DBNull.Value);
        command.Parameters.AddWithValue("@idDoctor", appointment.IdDoctor);
        command.Parameters.AddWithValue("@Date", appointment.AppointmentDate);
        command.Parameters.AddWithValue("@idPatient", appointment.IdPatient);

        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            if (await reader.ReadAsync(cancellationToken))
            {
                var doctorExists = (int)reader["DoctorExists"] > 0;
                var patientExists = (int)reader["PatientExists"] > 0;
                var dateCondition = (int)reader["DateCondition"] == 1;

                if (!doctorExists) throw new BadConditionException("Doctor not found or not active");
                if (!patientExists) throw new BadConditionException("Patient not found or not active");
                if (!dateCondition) throw new BadConditionException("Date can't be in the past");
            }
        }
        command.Parameters.Clear();
        try
        {
            command.CommandText = """"
                                  INSERT INTO Appointments (IdPatient, IdDoctor, AppointmentDate, Status, Reason, InternalNotes)
                                  OUTPUT INSERTED.IdAppointment
                                  VALUES (@IdP, @IdD, @Date, 'Scheduled', @Reason, @Notes)
                                  """";
            command.Parameters.AddWithValue("@IdP", appointment.IdPatient);
            command.Parameters.AddWithValue("@IdD", appointment.IdDoctor);
            command.Parameters.AddWithValue("@Date", appointment.AppointmentDate);
            command.Parameters.AddWithValue("@Reason", appointment.Reason);
            command.Parameters.AddWithValue("@Notes", (object?)appointment.InternalNotes ?? DBNull.Value);
            var newId = (int)await command.ExecuteScalarAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return await GetAppointmentById(newId, cancellationToken);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task RemoveAppointment(int idAppointment, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(configuration.GetConnectionString("Default"));
        await using var command = new SqlCommand();
        command.Connection = connection;
        await connection.OpenAsync(cancellationToken);
        command.CommandText = """
                               SELECT (SELECT COUNT(*) FROM appointments WHERE IdAppointment = @idAppointment) AS IsExisting,
                               (SELECT COUNT(*) FROM appointments WHERE IdAppointment = @idAppointment AND Status = 'Completed') AS IsCompleted
                               
                               """;
        command.Parameters.AddWithValue("@IdAppointment", idAppointment);
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            if (await reader.ReadAsync(cancellationToken))
            {
                var isExisting = (int)reader["IsExisting"] > 0;
                var IsCompleted = (int)reader["IsCompleted"] > 0;

                if (IsCompleted) throw new BadConditionException("Cannot delete a completed appointment");
            }
        }
        command.Parameters.Clear();
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        command.Transaction = (SqlTransaction)transaction;
        try
        {
            command.CommandText = "delete from Appointments where IdAppointment = @idAppointment";
            command.Parameters.AddWithValue("@idAppointment", idAppointment);
            await command.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task UpdateAppointment(int id, UpdateAppointmentRequestDto appointment,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(configuration.GetConnectionString("Default"));
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand();
        command.Connection = connection;

        command.CommandText = """
                              SELECT
                                (SELECT CASE WHEN @Date > GETDATE() THEN 1 ELSE 0 END) AS DateCondition,
                                (SELECT COUNT(*) FROM Appointments WHERE IdAppointment = @IdApp) AS IsExisting,
                                (SELECT COUNT(*) FROM Appointments WHERE IdAppointment = @IdApp AND Status = 'Completed') AS IsCompleted,
                                (SELECT COUNT(*) FROM Patients WHERE IdPatient = @IdPat AND IsActive = 1) AS PatientExist,
                                (SELECT CASE WHEN @Status IN ('Scheduled','Completed','Cancelled') THEN 1 ELSE 0 END) AS StatusCondition,
                                (SELECT COUNT(*) FROM Doctors WHERE IdDoctor = @IdDoc AND IsActive = 1) AS DoctorExists,
                                (SELECT CASE WHEN NOT EXISTS (
                                    SELECT 1 FROM Appointments 
                                    WHERE IdDoctor = @IdDoc AND AppointmentDate = @Date AND IdAppointment != @IdApp
                                ) THEN 1 ELSE 0 END) AS NoConflict
                              """;

        command.Parameters.AddWithValue("@IdApp", id);
        command.Parameters.AddWithValue("@IdDoc", appointment.IdDoctor);
        command.Parameters.AddWithValue("@Date", appointment.AppointmentDate);
        command.Parameters.AddWithValue("@IdPat", appointment.IdPatient);
        command.Parameters.AddWithValue("@Status", appointment.Status);

        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            if (await reader.ReadAsync(cancellationToken))
            {
                if ((int)reader["IsExisting"] == 0) throw new NotFoundException("Appointment does not exist");
                if ((int)reader["IsCompleted"] == 1)
                    throw new BadConditionException("Cannot update a completed appointment");
                if ((int)reader["DoctorExists"] == 0)
                    throw new NotFoundException("Doctor does not exist or is not active");
                if ((int)reader["PatientExist"] == 0)
                    throw new NotFoundException("Patient does not exist or is not active");
                if ((int)reader["StatusCondition"] == 0) throw new BadConditionException("Invalid Status name");
                if ((int)reader["DateCondition"] == 0)
                    throw new BadConditionException("New date cannot be in the past");
                if ((int)reader["NoConflict"] == 0)
                    throw new BadConditionException("Doctor has a conflict at this time");
            }
        }

        command.Parameters.Clear();
        command.CommandText = """
                              UPDATE Appointments
                              SET 
                                  IdDoctor = @IdD,
                                  AppointmentDate = @Date,
                                  Status = @Status,
                                  Reason = @Reason,
                                  InternalNotes = @Notes,
                                  IdPatient = @IdP
                              WHERE IdAppointment = @IdApp
                              """;

        command.Parameters.AddWithValue("@IdApp", id);
        command.Parameters.AddWithValue("@IdD", appointment.IdDoctor);
        command.Parameters.AddWithValue("@Date", appointment.AppointmentDate);
        command.Parameters.AddWithValue("@Status", appointment.Status);
        command.Parameters.AddWithValue("@Reason", appointment.Reason);
        command.Parameters.AddWithValue("@IdP", appointment.IdPatient);
        command.Parameters.AddWithValue("@Notes", (object?)appointment.InternalNotes ?? DBNull.Value);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}