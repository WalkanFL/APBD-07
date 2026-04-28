using System.Data;
using APBD_07.DTOs;
using Microsoft.Data.SqlClient;

namespace APBD_07.Services;

public class AppointmentService : IAppointmentService
{
    public readonly string _connectionString;

    public AppointmentService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    public async Task<IEnumerable<AppointmentListDTO>> GetAllAppointmentsAsync(string? status, string? patientLastName)
    {
        var query = "SELECT a.IdAppointment, a.AppointmentDate, a.Status, a.Reason, p.FirstName + N' ' + p.LastName AS PatientFullName, p.Email AS PatientEmail FROM dbo.Appointments a JOIN dbo.Patients p ON p.IdPatient = a.IdPatient WHERE (@Status IS NULL OR a.Status = @Status)  AND (@PatientLastName IS NULL OR p.LastName = @PatientLastName) ORDER BY a.AppointmentDate;"; //sql

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand();
        command.Connection = connection;
        command.CommandText = query;
        
        //cast na object bo status i dbnull.value muszą być tego samego typu a sprawdzamy tylko czy to null
        command.Parameters.AddWithValue("@Status", (object?)status ?? DBNull.Value);
        command.Parameters.AddWithValue("@PatientLastName", (object?)patientLastName ?? DBNull.Value);


        List<AppointmentListDTO> appointList = new List<AppointmentListDTO>();

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            appointList.Add(new AppointmentListDTO
            {
                IdAppointment = reader.GetInt32(reader.GetOrdinal("IdAppointment")),
                AppointmentDate = reader.GetDateTime(reader.GetOrdinal("AppointmentDate")),
                Status = reader.GetString(reader.GetOrdinal("Status")),
                Reason = reader.IsDBNull(reader.GetOrdinal("Reason")) ? null : reader.GetString(reader.GetOrdinal("Reason")),
                PatientFullName = reader.GetString(reader.GetOrdinal("PatientFullName")),
                PatientEmail = reader.GetString(reader.GetOrdinal("PatientEmail"))
            });
        }

        return appointList;

    }
    
    public async Task<AppointmentDetailsDTO?> GetAppointmentAsync(int id)
    { 
        var query = " SELECT a.IdAppointment, a.IdPatient, a.IdDoctor, a.AppointmentDate, a.Status, a.Reason, a.InternalNotes, a.CreatedAt, p.Email FROM dbo.Appointments a JOIN dbo.Patients p ON a.IdPatient = p.IdPatient WHERE a.IdAppointment = @Id";
        
        await using var connection = new SqlConnection(_connectionString); 
        await using var command = new SqlCommand(query, connection); 
        command.Parameters.AddWithValue("@Id", id);
        
        await connection.OpenAsync(); 
        await using var reader = await command.ExecuteReaderAsync();
        
        if (await reader.ReadAsync()) 
        { 
            return new AppointmentDetailsDTO 
            { 
                IdAppointment = reader.GetInt32(reader.GetOrdinal("IdAppointment")), 
                IdPatient = reader.GetInt32(reader.GetOrdinal("IdPatient")), 
                IdDoctor = reader.GetInt32(reader.GetOrdinal("IdDoctor")), 
                Email = reader.GetString(reader.GetOrdinal("Email")), 
                AppointmentDate = reader.GetDateTime(reader.GetOrdinal("AppointmentDate")), 
                Status = reader.GetString(reader.GetOrdinal("Status")), 
                Reason = reader.IsDBNull(reader.GetOrdinal("Reason")) ? null : reader.GetString(reader.GetOrdinal("Reason")),
                InternalNotes = reader.IsDBNull(reader.GetOrdinal("InternalNotes")) ? null : reader.GetString(reader.GetOrdinal("InternalNotes")), 
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                
            };
        }
        return null;
    }
    
    public async Task<int> PostAppointmentAsync(CreateAppointmentRequestDTO request)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        //weryfikacja wartości
        var checkQuery = @"SELECT (SELECT COUNT(*) FROM dbo.Patients WHERE IdPatient = @IdPatient AND IsActive = 1) AS PatientActive, (SELECT COUNT(*) FROM dbo.Doctors WHERE IdDoctor = @IdDoctor AND IsActive = 1) AS DoctorActive, (SELECT COUNT(*) FROM dbo.Appointments WHERE IdDoctor = @IdDoctor AND AppointmentDate = @Date AND Status = 'Scheduled') AS DoctorBusy";
   
        await using var checkCommand = new SqlCommand(checkQuery, connection);
        checkCommand.Parameters.AddWithValue("@IdPatient", request.IdPatient);
        checkCommand.Parameters.AddWithValue("@IdDoctor", request.IdDoctor);
        checkCommand.Parameters.AddWithValue("@Date", request.AppointmentDate);

        using var reader = await checkCommand.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            if (reader.GetInt32(reader.GetOrdinal("DoctorBusy")) > 0){ return -1;} // badReq: Termin zajęty
            if (reader.GetInt32(reader.GetOrdinal("PatientActive")) == 0){ return -2;}  // badReq: Pacjent nieaktywny
            if (reader.GetInt32(reader.GetOrdinal("DoctorActive")) == 0){ return -3;}  // badReq: Lekarz -//-
        }
        await reader.CloseAsync();
        
        var insertQuery = "INSERT INTO Appointments (IdPatient, IdDoctor, AppointmentDate, Status, Reason)VALUES (@IdPatient, @IdDoctor, @Date, 'Scheduled', @Reason);";
        
        await using var insertCommand = new SqlCommand(insertQuery, connection);
        insertCommand.Parameters.AddWithValue("@IdPatient", request.IdPatient);
        insertCommand.Parameters.AddWithValue("@IdDoctor", request.IdDoctor);
        insertCommand.Parameters.AddWithValue("@Date", request.AppointmentDate);
        insertCommand.Parameters.AddWithValue("@Reason", request.Reason);

        return (int)await insertCommand.ExecuteScalarAsync();
    }
    
    public async Task<int> PutAppointmentAsync(UpdateAppointmentRequestDTO request, int id)
    {
    await using var connection = new SqlConnection(_connectionString);
    await connection.OpenAsync();

    var currentQuery = "SELECT Status, AppointmentDate FROM dbo.Appointments WHERE IdAppointment = @Id";
    await using var checkCurrentCommand = new SqlCommand(currentQuery, connection);
    checkCurrentCommand.Parameters.AddWithValue("@Id", id);

    string? currentStatus = null;
    DateTime? currentDate = null;

    using (var reader = await checkCurrentCommand.ExecuteReaderAsync())
    {
        if (!await reader.ReadAsync()) return 0; // not found
        currentStatus = reader.GetString(0);
        currentDate = reader.GetDateTime(1);
    }

    if (currentStatus == "Completed" && currentDate != request.AppointmentDate) return -4; // badReq: Zmiana daty przy completed
    
    var checkQuery = "SELECT (SELECT COUNT(*) FROM dbo.Patients WHERE IdPatient = @IdPatient AND IsActive = 1) AS PatientActive, (SELECT COUNT(*) FROM dbo.Doctors WHERE IdDoctor = @IdDoctor AND IsActive = 1) AS DoctorActive, (SELECT COUNT(*) FROM dbo.Appointments WHERE IdDoctor = @IdDoctor AND AppointmentDate = @Date AND Status = 'Scheduled' AND IdAppointment != @Id) AS DoctorBusy";
    
    await using var checkCommand = new SqlCommand(checkQuery, connection);
    checkCommand.Parameters.AddWithValue("@IdPatient", request.IdPatient);
    checkCommand.Parameters.AddWithValue("@IdDoctor", request.IdDoctor);
    checkCommand.Parameters.AddWithValue("@Date", request.AppointmentDate);
    checkCommand.Parameters.AddWithValue("@Id", id);

    using (var reader = await checkCommand.ExecuteReaderAsync())
    {
        if (await reader.ReadAsync())
        { 
            if (reader.GetInt32(reader.GetOrdinal("DoctorBusy")) > 0){ return -1;} // conflict: Termin zajęty
            if (reader.GetInt32(reader.GetOrdinal("PatientActive")) == 0){ return -2;}  // badReq: Pacjent nieaktywny
            if (reader.GetInt32(reader.GetOrdinal("DoctorActive")) == 0){ return -3;}  // badReq: Lekarz -//-
        }
    }
    
    var updateQuery = "UPDATE Appointments SET IdPatient = @IdPatient, IdDoctor = @IdDoctor, AppointmentDate = @Date, Status = @Status, Reason = @Reason, InternalNotes = @Notes WHERE IdAppointment = @Id";
    
    await using var updateCmd = new SqlCommand(updateQuery, connection);
    updateCmd.Parameters.AddWithValue("@IdPatient", request.IdPatient);
    updateCmd.Parameters.AddWithValue("@IdDoctor", request.IdDoctor);
    updateCmd.Parameters.AddWithValue("@Date", request.AppointmentDate);
    updateCmd.Parameters.AddWithValue("@Status", request.Status);
    updateCmd.Parameters.AddWithValue("@Reason", request.Reason);
    updateCmd.Parameters.AddWithValue("@Notes", (object?)request.InternalNotes ?? DBNull.Value);
    
    updateCmd.Parameters.AddWithValue("@Id", id);

    await updateCmd.ExecuteNonQueryAsync();
    return 1; // Ok: Sukces
    }
    
    public async Task<int> DeleteAppointmentAsync(int id)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var checkQuery = "SELECT (SELECT COUNT(*) FROM dbo.Appointments WHERE IdAppointment = @Id) AS QueryExists, (SELECT COUNT(*) FROM dbo.Appointments WHERE IdAppointment = @Id AND Status = 'Completed') AS IsCompleted";
        await using var checkCommand = new SqlCommand(checkQuery, connection);

        checkCommand.Parameters.AddWithValue("@Id", id);

        using var reader = await checkCommand.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            if (reader.GetInt32(reader.GetOrdinal("QueryExists")) == 0){ return 0;} // not found
            if (reader.GetInt32(reader.GetOrdinal("IsCompleted")) > 0){ return -1;}  // conflict: completed
        }
        await reader.CloseAsync();
        
        var query = "DELETE FROM dbo.Appointments WHERE IdAppointment = @Id";
        await using var command = new SqlCommand(query, connection);
        
        command.Parameters.AddWithValue("@Id", id);
        await command.ExecuteNonQueryAsync();
        return 1; //204 : no content
    }
    
}