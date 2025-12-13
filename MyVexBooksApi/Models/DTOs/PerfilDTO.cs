namespace MyVexBooks.Models.DTOs
{
    public class PerfilDTO
    {
        public int IdUsuario { get; set; }
        public string Nombre { get; set; } = null!;
        public string Correo { get; set; } = null!;
        public DateTime FechaRegistro { get; set; }
    }
}
