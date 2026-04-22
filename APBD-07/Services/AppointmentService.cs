using System.Data;
using APBD_07.DTOs;
using Microsoft.Data.SqlClient;

namespace APBD_07.Services;

public class AppointmentService : IAppointmentService
{
    private readonly string _connectionString;

    public AppointmentService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    public async Task<IEnumerable<AppointmentListDTO>> GetAllAppointmentsAsync()
    {
        var query = ""; //sql

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand();
        command.Connection = connection;
        command.CommandText = query;

        List<AppointmentListDTO> appointList = new List<AppointmentListDTO>();

        return null;

    }
    
}