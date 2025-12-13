using Microsoft.EntityFrameworkCore;
using MyVexBooks.Models.DTOs;
using MyVexBooks.Models.Entities;

namespace MyVexBooks.Repositories
{
    public class LibrosRepository : Repository<Libros>, ILibrosRepository
    {
        public LibrosRepository(LibrosvexContext context) : base(context)
        {
            
        }

        public Libros? GetConPartes(int id)
        {
            return Context.Libros
                          .Include(l => l.Partes) 
                          .FirstOrDefault(l => l.IdLibro == id);
        }

        public Partes? GetParte(int idParte)
        {
            return Context.Partes
                          .FirstOrDefault(p => p.IdParte == idParte);
        }

        public List<Libros> GetLibrosConPartesPorGenero(string genero)
        {
            return Context.Libros
                .Include(l => l.Partes)
                .Where(l => l.Genero.ToLower().Contains(genero.ToLower()))
                .ToList();
        }


        public Partelikes? GetLikeParte(int idParte, int idUsuario)
        {
            return Context.Partelikes.FirstOrDefault(pl => pl.IdParte == idParte && pl.IdUsuario == idUsuario);
        }

        public void AgregarLike(Partelikes like)
        {
            Context.Partelikes.Add(like);
        }

        public void EliminarLike(Partelikes like)
        {
            Context.Partelikes.Remove(like);
        }

        public void GuardarCambios()
        {
            Context.SaveChanges();
        }

    }

}
