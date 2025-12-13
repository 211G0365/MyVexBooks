namespace MyVexBooks.Models.DTOs
{
    public interface ILibroDTO
    {
        int IdLibro { get; set; }
        string Titulo { get; set; }
        string Autor { get; set; }
        string Genero { get; set; }
        string Sinopsis { get; set; }
        string PortadaURL { get; set; }
    }
}
