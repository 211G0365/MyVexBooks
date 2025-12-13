using System;
using System.Collections.Generic;

namespace MyVexBooks.Models.Entities;

public partial class Partes
{
    public int IdParte { get; set; }

    public int IdLibro { get; set; }

    public int? Likes { get; set; }

    public int NumeroParte { get; set; }

    public string? NombreParte { get; set; }

    public string Contenido { get; set; } = null!;

    public virtual Libros IdLibroNavigation { get; set; } = null!;

    public virtual ICollection<Partelikes> Partelikes { get; set; } = new List<Partelikes>();
}
