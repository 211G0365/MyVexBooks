using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyVexBooks.Models.DTOs;
using MyVexBooks.Models.Entities;
using MyVexBooks.Repositories;

namespace MyVexBooks.Controllers.api
{
    [Route("api/[controller]")]
    [ApiController]
    public class LibrosController : ControllerBase
    {
        private readonly ILibrosRepository repository;
        private readonly IMapper mapper;

        public LibrosController(ILibrosRepository repository, IMapper mapper )
        {
            this.repository = repository;
            this.mapper = mapper;
        }

        // LISTA DE LIBROS
        [HttpGet]
        public IActionResult ObtenerLibros()
        {
            var libros = repository.GetAll();
            var librosDTO = mapper.Map<List<LibroDTO>>(libros);
            return Ok(librosDTO);
        }



        // LIBRO COMPLETO
        [HttpGet("{id}")]
        public IActionResult ObtenerLibro(int id)
        {
            var libro = repository.GetConPartes(id);

            if (libro == null)
                return NotFound(new { mensaje = "Libro no encontrado" });

            libro.Partes = libro.Partes.OrderBy(p => p.NumeroParte).ToList();

          
            var totalLikes = libro.Partes.Sum(p => p.Likes ?? 0);

            var libroDTO = mapper.Map<LibroCompletoDTO>(libro);
            libroDTO.Likes = totalLikes; 

            return Ok(libroDTO);
        }



        // LIBROS POR GENERO
        [HttpGet("genero/{genero}")]
        public IActionResult ObtenerPorGenero(string genero)
        {
            var libros = repository.GetLibrosConPartesPorGenero(genero);

            var listaDTO = new List<LibroCompletoDTO>();

            foreach (var libro in libros)
            {
                libro.Partes = libro.Partes.OrderBy(p => p.NumeroParte).ToList();
                var totalLikes = libro.Partes.Sum(p => p.Likes ?? 0);

                var dto = mapper.Map<LibroCompletoDTO>(libro);
                dto.Likes = totalLikes;

                listaDTO.Add(dto);
            }

            return Ok(listaDTO);
        }





        // LIBROS RECIENTES
        [HttpGet("recientes")]
        public IActionResult ObtenerRecientes()
        {
            var libros = repository.GetAll()
                .OrderByDescending(l => l.IdLibro)
                .Take(20);

            return Ok(mapper.Map<List<LibroDTO>>(libros));
        }


        //BUCADOR DE LIBROS
        [HttpGet("buscar/{texto}")]
        public IActionResult Buscar(string texto)
        {
            texto = texto.ToLower();

            var libros = repository.GetAll()
                .Where(l =>
                    l.Titulo.ToLower().Contains(texto) ||
                    l.Autor.ToLower().Contains(texto) ||
                    l.Genero.ToLower().Contains(texto));

            return Ok(mapper.Map<List<LibroDTO>>(libros));
        }


        // LISTA DE GENEROS
        [HttpGet("generos")]
        public IActionResult ObtenerGeneros()
        {
            var generos = repository.GetAll()
                .Select(l => l.Genero)
                .Where(g => !string.IsNullOrWhiteSpace(g))
                .SelectMany(g => g.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(g => g.Trim())
                .Distinct()
                .ToList();

            return Ok(generos);
        }

        [HttpGet("parte/{idParte}")]
        public IActionResult ObtenerParte(int idParte)
        {
            var parte = repository.GetParte(idParte);

            if (parte == null)
                return NotFound(new { mensaje = "Parte no encontrada" });

            return Ok(mapper.Map<ParteDTO>(parte));
        }


  
        [HttpGet("parte/{idParte}/like")]
        public IActionResult VerificarLike(int idParte)
        {
            int idUsuario = ObtenerUsuarioId(); 

            var like = repository.GetLikeParte(idParte, idUsuario);

            return Ok(new { liked = like != null });
        }


        [HttpPost("parte/{idParte}/like")]
        public IActionResult ToggleLike(int idParte)
        {
            int idUsuario = ObtenerUsuarioId();

            var parte = repository.GetParte(idParte);
            if (parte == null)
                return NotFound(new { mensaje = "Parte no encontrada" });

            var likeExistente = repository.GetLikeParte(idParte, idUsuario);

            if (likeExistente != null)
            {
      
                repository.EliminarLike(likeExistente);
                parte.Likes = (parte.Likes ?? 1) - 1;
            }
            else
            {
      
                var nuevoLike = new Partelikes
                {
                    IdParte = idParte,
                    IdUsuario = idUsuario,
                    FechaLike = DateTime.Now
                };
                repository.AgregarLike(nuevoLike);
                parte.Likes = (parte.Likes ?? 0) + 1;
            }

            repository.GuardarCambios();

            return Ok(new { likes = parte.Likes, liked = likeExistente == null });
        }

        private int ObtenerUsuarioId()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "IdUsuario")?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                throw new Exception("No se encontró el claim 'IdUsuario' en el token.");

            return int.Parse(userIdClaim);
        }


    }

}
