using AutoMapper;
using MyVexBooks.Models.DTOs;
using MyVexBooks.Models.Entities;
using MyVexBooks.Repositories;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MyVexBooks.Helpers;

namespace MyVexBooks.Services
{
    public class UsuarioService : IUsuarioService
    {

        private readonly JwtHelper jwtHelper;

        public UsuarioService(IRepository<Usuarios> repository, IMapper mapper, JwtHelper jwtHelper)
        {
            Repository = repository;
            Mapper = mapper;
            this.jwtHelper = jwtHelper;
        }

        public IRepository<Usuarios> Repository { get; }
        public IMapper Mapper { get; }




        public void RegistrarUsuario(IRegistroDTO dto)
        {
        
            var entidad = Mapper.Map<Usuarios>(dto);
          
            entidad.ContraseñaHash = EncriptacionHelper.GetHash(dto.Contraseña);
            entidad.Correo = entidad.Correo.Trim().ToLower();

            entidad.FechaRegistro = DateTime.UtcNow;   

   
            Repository.Insert(entidad);
        }


        // LOGIN
        public string IniciarSesion(IIniciarSesionDTO dto)
        {
            var hash = EncriptacionHelper.GetHash(dto.Contraseña);
            var entidad = Repository.GetAll()
                .FirstOrDefault(x =>
                    x.Correo.ToLower() == dto.Correo.Trim().ToLower()
                    && x.ContraseñaHash == hash);

            if (entidad == null)
                return string.Empty;

            // Claims
            List<Claim> claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, entidad.Nombre),
            new Claim("Nombre", entidad.Nombre),

            new Claim(ClaimTypes.NameIdentifier, entidad.IdUsuario.ToString()),
            new Claim("IdUsuario", entidad.IdUsuario.ToString()),

            new Claim("Correo", entidad.Correo)
        };

     
            return jwtHelper.GenerateJwtToken(claims);
        }

        public PerfilDTO ObtenerPerfil(int idUsuario)
        {
            var usuario = Repository.Get(idUsuario);
            if (usuario == null)
                throw new Exception("Usuario no encontrado");

            return new PerfilDTO
            {
                IdUsuario = usuario.IdUsuario,
                Nombre = usuario.Nombre,
                Correo = usuario.Correo,
                FechaRegistro = usuario.FechaRegistro
            };
        }

        public void ActualizarNombre(int idUsuario, string nombre)
        {
            var usuario = Repository.Get(idUsuario);
            if (usuario == null) throw new Exception("Usuario no encontrado");

            usuario.Nombre = nombre.Trim();
            Repository.Update(usuario);
        }

        public void ActualizarCorreo(int idUsuario, string correo)
        {
            correo = correo.Trim().ToLower();

            var existe = Repository.GetAll().Any(u => u.Correo == correo && u.IdUsuario != idUsuario);
            if (existe) throw new Exception("El correo ya está registrado");

            var usuario = Repository.Get(idUsuario);
            if (usuario == null) throw new Exception("Usuario no encontrado");

            usuario.Correo = correo;
            Repository.Update(usuario);
        }

        public void CambiarContraseña(int idUsuario, string nueva)
        {
            var usuario = Repository.Get(idUsuario);
            if (usuario == null) throw new Exception("Usuario no encontrado");

            usuario.ContraseñaHash = EncriptacionHelper.GetHash(nueva);
            Repository.Update(usuario);
        }

      




    }
}
