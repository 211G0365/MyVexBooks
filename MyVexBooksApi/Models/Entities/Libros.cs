using System;
using System.Collections.Generic;

namespace MyVexBooks.Models.Entities;

public partial class Libros
{
    public int IdLibro { get; set; }

    public string Titulo { get; set; } = null!;

    public string Autor { get; set; } = null!;

    public string Genero { get; set; } = null!;

    public string? Sinopsis { get; set; }

    public string? PortadaUrl { get; set; }

    public virtual ICollection<Partes> Partes { get; set; } = new List<Partes>();
}
