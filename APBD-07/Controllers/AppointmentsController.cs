using APBD_07.DTOs;
using APBD_07.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace APBD_07.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppointmentsController : ControllerBase
    {
        private readonly IAppointmentService _appointmentService;
        public AppointmentsController(IAppointmentService appointmentService)
        {
            _appointmentService = appointmentService;
        }
        
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string? status, [FromQuery] string? patientLastName)
        {
            var appointments = await _appointmentService.GetAllAppointmentsAsync(status,patientLastName);
            if (appointments.Any())
            {
                return Ok(appointments);
            }
            return NotFound(new ErrorResponseDTO{Message= "No matching appointments"});

        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get([FromRoute] int id)
        {
            var appointment = await _appointmentService.GetAppointmentAsync(id);

            if (appointment == null)
            {
                return NotFound(new ErrorResponseDTO{Message= $"Appointment of ID {id} doesn't exist."});
            }
            return Ok(appointment);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody]CreateAppointmentRequestDTO request)
        {
            //data sprawdzana tutaj, dto handluje długość powodu
            if (request.AppointmentDate < DateTime.Now)
            {
                return BadRequest(new ErrorResponseDTO{Message="Tried requesting appointment in the past."});
            }     
            
            var result = await _appointmentService.PostAppointmentAsync(request);
            
            switch (result)
            {
                case -1:
                    return Conflict(new ErrorResponseDTO { Message = "Doctor is busy" }); 
                case -2:
                    return BadRequest(new ErrorResponseDTO { Message = "Incorrect Patient" });
                case -3:
                    return BadRequest(new ErrorResponseDTO { Message = "Incorrect Doctor" });
                default:
                    return CreatedAtAction("Post",request);
            }

        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put([FromBody] UpdateAppointmentRequestDTO request, [FromRoute] int id)
        {
            var allowedStatuses = new[] {"Scheduled", "Completed", "Cancelled"};
            if (!allowedStatuses.Contains(request.Status))
            {
                return BadRequest(new ErrorResponseDTO{Message = "Non-standard Status"});
            }

            var result = await _appointmentService.PutAppointmentAsync(request, id);
            
            switch (result)
            {
                case 0:
                    return NotFound(new ErrorResponseDTO { Message = $"Appointment of id: {id} not found" });
                case -1:
                    return Conflict(new ErrorResponseDTO { Message = "Doctor is busy" }); 
                case -2:
                    return BadRequest(new ErrorResponseDTO { Message = "Incorrect Patient" });
                case -3:
                    return BadRequest(new ErrorResponseDTO { Message = "Incorrect Doctor" });
                case -4:
                    return Conflict(new ErrorResponseDTO { Message = "Tried changing date of completed appointment" });
                case 1:
                    return Ok(request);
                default:
                    return null; //powinien być error dla nieprzewidzianych błędów
            }

        }
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            var result = await _appointmentService.DeleteAppointmentAsync(id);

            switch (result)
            {
                case 0:
                    return NotFound(new ErrorResponseDTO { Message = $"Appointment of id: {id} not found" });
                case -1:
                    return Conflict(new ErrorResponseDTO { Message = "No reason to delete completed appointment"});
                case 1:
                    return NoContent();
                default:
                    return null;
            }
        }
    }
}
