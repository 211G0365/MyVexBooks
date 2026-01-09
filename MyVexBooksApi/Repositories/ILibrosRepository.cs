using MyVexBooks.Models.Entities;

namespace MyVexBooks.Repositories
{
    public interface ILibrosRepository : IRepository<Libros>
    {
        Libros? GetConPartes(int id);
        Partes? GetParte(int idParte);

        List<Libros> GetLibrosConPartesPorGenero(string genero);

        Partelikes? GetLikeParte(int idParte, int idUsuario);
        void AgregarLike(Partelikes like);
        void EliminarLike(Partelikes like);
        void GuardarCambios();
        List<Libros> GetRecientes(int cantidad = 10);
    }
}
