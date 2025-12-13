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
            return Ok(Service.GetPublicKey());
        }

        [HttpPost]
        public IActionResult Post(SubscriptionDTO dto)
        {
     

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
