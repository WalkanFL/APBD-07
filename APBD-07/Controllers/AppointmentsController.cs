using APBD_07.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace APBD_07.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppointmentsController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return null;
        }

        [HttpGet("{id:int}")]
        public IActionResult Get([FromRoute] int id)
        {
            return null;
        }

        [HttpPost]
        public IActionResult Post([FromBody]AppointmentDetailsDTO appointment)
        {
            return null;
        }

        [HttpPut("{id:int}")]
        public IActionResult Put([FromBody] AppointmentDetailsDTO appointment, [FromRoute] int id)
        {
            return null;
        }
        [HttpDelete("{id:int}")]
        public IActionResult Delete([FromRoute] int id)
        {
            return null;
        }
    }
}
