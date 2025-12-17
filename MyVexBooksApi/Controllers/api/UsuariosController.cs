using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyVexBooks.Models.Dtos;
using MyVexBooks.Models.DTOs;
using MyVexBooks.Services;

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
        public async Task<IActionResult> SubirFoto([FromForm] FotoPerfilDTO dto)
        {
            try
            {
                var archivo = dto.Archivo;

                if (archivo == null || archivo.Length == 0)
                    return BadRequest(new { error = "No se envió ninguna imagen" });

                var idUsuario = int.Parse(User.FindFirst("IdUsuario")!.Value);

                var carpeta = Path.Combine(env.WebRootPath, "fotoPerfil");
                if (!Directory.Exists(carpeta))
                    Directory.CreateDirectory(carpeta);

                var extension = Path.GetExtension(archivo.FileName);
                var nombreArchivo = $"usuario_{idUsuario}{extension}";
                var ruta = Path.Combine(carpeta, nombreArchivo);

   
                using (var stream = new FileStream(ruta, FileMode.Create))
                {
                    await archivo.CopyToAsync(stream);
                }
                var urlPublica = $"{Request.Scheme}://{Request.Host}/fotoPerfil/{nombreArchivo}";

                return Ok(new { fotoURL = urlPublica });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }




        [Authorize]
        [HttpGet("perfil/foto")]
        public IActionResult ObtenerFoto()
        {
            var idUsuario = int.Parse(User.FindFirst("IdUsuario")!.Value);

            var carpeta = Path.Combine(env.WebRootPath, "fotoPerfil");

            var archivos = Directory.GetFiles(carpeta, $"usuario_{idUsuario}.*");

            if (archivos.Length > 0)
            {
                var nombreArchivo = Path.GetFileName(archivos[0]);
                var urlPublica = $"https://{Request.Host}/fotoPerfil/{nombreArchivo}";
                return Ok(new { fotoURL = urlPublica });
            }

            var defaultURL = $"https://{Request.Host}/fotoPerfil/perfil.png";
            return Ok(new { fotoURL = defaultURL });
        }




    }
}
