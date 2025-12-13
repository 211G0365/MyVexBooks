namespace MyVexBooks.Models.DTOs
{
    public interface IParteDTO
    {
        int IdParte { get; set; }
        int IdLibro { get; set; }        
        int NumeroParte { get; set; }
        string? NombreParte { get; set; } 
        string Contenido { get; set; }

 
    }
}

