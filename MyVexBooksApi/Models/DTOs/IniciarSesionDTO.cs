using MyVexBooks.Models.DTOs;

namespace MyVexBooks.Models.Dtos
{
    public class IniciarSesionDTO : IIniciarSesionDTO
    {
        public string Correo { get; set; } = null!;
        public string Contraseña { get; set; } = null!;

    }
}
