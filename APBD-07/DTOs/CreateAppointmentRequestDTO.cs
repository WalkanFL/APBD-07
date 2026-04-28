using System.ComponentModel.DataAnnotations;

namespace APBD_07.DTOs;

public class CreateAppointmentRequestDTO
{
    public int IdPatient { get; set; }
    public int IdDoctor { get; set; }
    public DateTime AppointmentDate { get; set; }
    [MaxLength(250)]
    public string Reason { get; set; } = string.Empty;
}