using MyVexBooks.Models.Entities;

namespace MyVexBooks.Models.DTOs
{
    public class ParteLike
    {
        public int Id { get; set; }
        public int IdParte { get; set; }
        public int IdUsuario { get; set; }
        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        // NAVIGATIONS
        public virtual Partes Parte { get; set; } = null!;
    }
}
