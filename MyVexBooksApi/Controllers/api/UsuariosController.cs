using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyVexBooks.Models.Dtos;
using MyVexBooks.Models.DTOs;
using MyVexBooks.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace MyVexBooks.Controllers.api
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private readonly IUsuarioService service;
        private readonly IWebHostEnvironment env;

        public UsuariosController(IUsuarioService service, IWebHostEnvironment env)
        {
            this.service = service;
            this.env = env;
        }

        //REGISTRO
        [HttpPost("registro")]
        public IActionResult RegistrarUsuario([FromBody] RegistroDTO dto)
        {
            try
            {
                service.RegistrarUsuario(dto);
                return Ok(new { mensaje = "Usuario registrado correctamente" });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    error = ex.Message,
                    inner = ex.InnerException?.Message
                });
            }
        }

        //LOGIN 
        [HttpPost("login")]
        public IActionResult IniciarSesion([FromBody] IniciarSesionDTO dto)
        {
            var token = service.IniciarSesion(dto);

            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(new { mensaje = "Correo electrónico o contraseña incorrecta" });
            }

            return Ok(new { token });
        }

        // PERFIL

        [HttpGet("perfil")]
        [Authorize]
        public IActionResult ObtenerPerfil()
        {
            var id = int.Parse(User.FindFirst("IdUsuario")!.Value);
            var perfil = service.ObtenerPerfil(id);
            return Ok(perfil);
        }

        [HttpPut("perfil/nombre")]
        [Authorize]
        public IActionResult ActualizarNombre([FromBody] ActualizarNombreDTO dto)
        {
            var id = int.Parse(User.FindFirst("IdUsuario")!.Value);
            service.ActualizarNombre(id, dto.Nombre);
            return Ok(new { mensaje = "Nombre actualizado" });
        }

        [HttpPut("perfil/correo")]
        [Authorize]
        public IActionResult ActualizarCorreo([FromBody] ActualizarCorreoDTO dto)
        {
            var id = int.Parse(User.FindFirst("IdUsuario")!.Value);
            service.ActualizarCorreo(id, dto.Correo);
            return Ok(new { mensaje = "Correo actualizado" });
        }

        [HttpPut("perfil/contraseña")]
        [Authorize]
        public IActionResult CambiarContraseña([FromBody] CambiarContraseñaDTO dto)
        {
            var id = int.Parse(User.FindFirst("IdUsuario")!.Value);

            service.CambiarContraseña(id, dto.Nueva); 

            return Ok(new { mensaje = "Contraseña actualizada" });
        }

        [Authorize]
        [HttpPost("perfil/foto")]
        public async Task<IActionResult> SubirFoto([FromForm(Name = "foto")] IFormFile Archivo)
        {
            if (Archivo == null || Archivo.Length == 0)
                return BadRequest(new { error = "No se envió ninguna imagen" });

            // 🛑 límite de peso (5 MB)
            if (Archivo.Length > 5 * 1024 * 1024)
                return BadRequest(new { error = "La imagen es demasiado grande" });

            var idUsuario = int.Parse(User.FindFirst("IdUsuario")!.Value);

            var carpeta = Path.Combine(env.WebRootPath, "fotoPerfil");
            if (!Directory.Exists(carpeta))
                Directory.CreateDirectory(carpeta);

            // 🔥 borrar fotos anteriores
            var anteriores = Directory.GetFiles(carpeta, $"usuario_{idUsuario}.*");
            foreach (var f in anteriores)
                System.IO.File.Delete(f);

            // 📥 cargar imagen
            using var image = await Image.LoadAsync(Archivo.OpenReadStream());

            // 🔧 redimensionar (cuadrada)
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(512, 512),
                Mode = ResizeMode.Crop
            }));

            // 📁 nombre fijo
            var nombreArchivo = $"usuario_{idUsuario}.webp";
            var ruta = Path.Combine(carpeta, nombreArchivo);

            // 💾 guardar como WEBP
            await image.SaveAsync(ruta, new WebpEncoder
            {
                Quality = 75
            });

            // 🧠 cache-buster para el frontend
            var url = $"{Request.Scheme}://{Request.Host}/fotoPerfil/{nombreArchivo}?v={DateTime.UtcNow.Ticks}";

            return Ok(new { fotoURL = url });
        }




        [Authorize]
        [HttpGet("perfil/foto")]
        public IActionResult ObtenerFoto()
        {
            var idUsuario = int.Parse(User.FindFirst("IdUsuario")!.Value);
            var carpeta = Path.Combine(env.WebRootPath, "fotoPerfil");

            var ruta = Path.Combine(carpeta, $"usuario_{idUsuario}.webp");

            if (System.IO.File.Exists(ruta))
            {
                var url = $"{Request.Scheme}://{Request.Host}/fotoPerfil/usuario_{idUsuario}.webp";
                return Ok(new { fotoURL = url });
            }

            var defaultURL = $"{Request.Scheme}://{Request.Host}/fotoPerfil/perfil.png";
            return Ok(new { fotoURL = defaultURL });
        }






    }
}
