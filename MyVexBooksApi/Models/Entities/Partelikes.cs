using System;
using System.Collections.Generic;

namespace MyVexBooks.Models.Entities;

public partial class Partelikes
{
    public int Id { get; set; }

    public int IdParte { get; set; }

    public int IdUsuario { get; set; }

    public DateTime FechaLike { get; set; }

    public virtual Partes IdParteNavigation { get; set; } = null!;

    public virtual Usuarios IdUsuarioNavigation { get; set; } = null!;
}
