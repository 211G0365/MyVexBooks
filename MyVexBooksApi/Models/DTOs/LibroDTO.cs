namespace MyVexBooks.Models.DTOs
{
    public class LibroDTO: ILibroDTO
    {
        public int IdLibro { get; set; }
        public string Titulo { get; set; } = null!;
        public string Autor { get; set; } = null!;
        public string Genero { get; set; } = null!;
        public string Sinopsis { get; set; } = null!;
        public string PortadaURL { get; set; } = null!;
    }
   
}
