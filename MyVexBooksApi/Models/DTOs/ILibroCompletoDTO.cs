using MyVexBooks.Models.DTOs;

public class ILibroCompletoDTO
{
    public int IdLibro { get; set; }
    public string Titulo { get; set; } = null!;
    public string Autor { get; set; } = null!;
    public string Genero { get; set; } = null!;
    public string Sinopsis { get; set; } = null!;
    public string PortadaURL { get; set; } = null!;

    public int Likes { get; set; } 
    public List<ParteDTO> Partes { get; set; } = new List<ParteDTO>();
}