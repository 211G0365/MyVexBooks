namespace MyVexBooks.Models.DTOs
{
    public class UsuarioDTO: IUsuarioDTO
    {
        public int IdUsuario { get; set; }
        public string Nombre { get; set; } = null!;
        public string Correo { get; set; } = null!;
        public string Token { get; set; } = null!;
    }
  
}
