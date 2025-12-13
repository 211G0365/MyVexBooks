namespace MyVexBooks.Models.DTOs
{
    public interface IUsuarioDTO
    {
        int IdUsuario { get; set; }
        string Nombre { get; set; }
        string Correo { get; set; }
        string Token { get; set; }
    }
}
