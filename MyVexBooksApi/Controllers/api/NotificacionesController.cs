using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyVexBooks.Models.DTOs;
using MyVexBooks.Repositories;
using MyVexBooks.Services;

namespace MyVexBooks.Controllers.api
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificacionesController : ControllerBase
    {
        public NotificacionesController(PushNotificationService service, ILibrosRepository repository)
        {
            Service = service;
            Repository = repository;
        }

        public PushNotificationService Service { get; }
        public ILibrosRepository Repository { get; }

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

        [AllowAnonymous]
        [HttpGet("probar2")]
        public async Task<IActionResult> Probar()
        {
            try
            {
                var libros = Repository.GetRecientes(10);
                if (libros == null || !libros.Any())
                    return BadRequest("No hay libros recientes para enviar.");


                var random = new Random();
                var libroRandom = libros[random.Next(libros.Count)];


                var payload = new
                {
                    titulo = "¡Nuevo libro reciente agregado!",
                    mensaje = $"Descubre este libro reciente: {libroRandom.Titulo}",
                    idLibro = libroRandom.IdLibro,
                    portada = string.IsNullOrEmpty(libroRandom.PortadaUrl)
          ? "img/logo.png"
          : libroRandom.PortadaUrl
                };

                Console.WriteLine($"Push enviado → Libro {libroRandom.IdLibro}");
                await Service.EnviarMensaje(payload);

                return Ok(new { mensaje = "Notificación enviada con libro reciente ", libro = libroRandom.Titulo });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error en Probar: " + ex);
                return StatusCode(500, "Error al enviar la notificación");
            }
        }


    }
}