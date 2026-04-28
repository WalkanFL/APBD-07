using APBD_07.DTOs;

namespace APBD_07.Services;

public interface IAppointmentService
{
    public async Task<IEnumerable<AppointmentListDTO>> GetAllAppointmentsAsync(string? status, string? patientLastName)
    {
        return null;
    }
    
    public async Task<AppointmentDetailsDTO?> GetAppointmentAsync(int id)
    {
        return null;
    }

    public async Task<int> PostAppointmentAsync(CreateAppointmentRequestDTO request)
    {
        return 0;
    }

    public async Task<int> PutAppointmentAsync(UpdateAppointmentRequestDTO request, int id)
    {
        return 0;
    }

    public async Task<int> DeleteAppointmentAsync(int id)
    {
        return 0;
    }

}