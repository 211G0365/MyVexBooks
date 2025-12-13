using AutoMapper;
using MyVexBooks.Models.DTOs;
using MyVexBooks.Models.Entities;
using MyVexBooks.Repositories;

namespace MyVexBooks.Services
{
    public interface IUsuarioService
    {
        IMapper Mapper { get; }
        IRepository<Usuarios> Repository { get; }

        string IniciarSesion(IIniciarSesionDTO dto);
        void RegistrarUsuario(IRegistroDTO dto);

        PerfilDTO ObtenerPerfil(int idUsuario);
        void ActualizarNombre(int idUsuario, string nombre);
        void ActualizarCorreo(int idUsuario, string correo);
       void CambiarContraseña(int idUsuario, string nueva);
      


    }
}
