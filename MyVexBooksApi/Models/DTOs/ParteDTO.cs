namespace MyVexBooks.Models.DTOs
{
    public class ParteDTO: IParteDTO
    {
        public int IdParte { get; set; }
        public int IdLibro { get; set; }      
        public int NumeroParte { get; set; }
        public string? NombreParte { get; set; } 
        public string Contenido { get; set; } = null!;
        public int Likes { get; set; }
        public bool DadoLike { get; set; }

        public string PortadaURL { get; set; } = null!; 
    }
    
}
