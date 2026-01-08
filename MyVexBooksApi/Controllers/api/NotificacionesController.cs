using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyVexBooks.Models.DTOs;
using MyVexBooks.Services;

namespace MyVexBooks.Controllers.api
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificacionesController : ControllerBase
    {
        public NotificacionesController(PushNotificationService service)
        {
            Service = service;
        }

        public PushNotificationService Service { get; }

        [HttpGet("publickey")]
        public IActionResult GetPublicKey()
        {
            try
            {
                var key = Service.GetPublicKey();
                return Ok(key);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error GetPublicKey: " + ex);
                return StatusCode(500, "Error interno al obtener la clave pública");
            }
        }



        [HttpPost]
        public IActionResult Post(SubscriptionDTO dto)
        {
            //Validar

            Service.Suscribir(dto);
            return Ok();
        }




        [HttpPost("desuscribir")]
        public IActionResult Desuscribir(SubscriptionDTO dto)
        {
            Service.Desuscribir(dto.Endpoint);
            return Ok();
        }

        [HttpGet("probar")]
        public async Task<IActionResult> Probar()
        {
            await Service.EnviarMensaje("Nuevo mensaje 🔔", "Tu primera notificación push funciona!");
            return Ok("Enviado");
        }
    }
}
