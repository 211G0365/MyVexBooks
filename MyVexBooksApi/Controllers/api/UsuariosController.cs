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

        private static readonly SemaphoreSlim _fotoLock = new(1, 1);
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
            await _fotoLock.WaitAsync();
            try
            {
                if (Archivo == null || Archivo.Length == 0)
                    return BadRequest(new { error = "No se envió ninguna imagen" });

                if (Archivo.Length > 5 * 1024 * 1024)
                    return BadRequest(new { error = "La imagen pesa más de 5MB" });

                var idUsuario = int.Parse(User.FindFirst("IdUsuario")!.Value);

                var carpeta = Path.Combine(env.WebRootPath, "fotoPerfil");
                Directory.CreateDirectory(carpeta);

                var nombreFinal = $"usuario_{idUsuario}.webp";
                var rutaFinal = Path.Combine(carpeta, nombreFinal);
                var rutaTemp = Path.Combine(carpeta, $"{Guid.NewGuid()}.tmp.webp");

                Image image;
                try
                {
                    await using var stream = Archivo.OpenReadStream();
                    image = await Image.LoadAsync(stream);
                }
                catch
                {
                    return BadRequest(new { error = "Imagen inválida o demasiado grande" });
                }

                if (image.Width > 6000 || image.Height > 6000)
                {
                    image.Dispose();
                    return BadRequest(new { error = "Resolución máxima: 6000x6000" });
                }

                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(512, 512),
                    Mode = ResizeMode.Crop
                }));

                await image.SaveAsync(rutaTemp, new WebpEncoder { Quality = 75 });
                image.Dispose();

                if (System.IO.File.Exists(rutaFinal))
                    System.IO.File.Delete(rutaFinal);

                System.IO.File.Move(rutaTemp, rutaFinal);

                return Ok(new { ok = true });
            }
            finally
            {
                _fotoLock.Release();
            }
        }




    }
}
