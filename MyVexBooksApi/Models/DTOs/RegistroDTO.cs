namespace MyVexBooks.Models.DTOs
{
    public class RegistroDTO : IRegistroDTO
    {
        public string Contraseña { get; set; } = null!;
        public string Correo { get; set; } = null!;
        public string Nombre { get; set; } = null!;
    }
}
