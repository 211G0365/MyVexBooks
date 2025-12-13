using System;
using System.Collections.Generic;

namespace MyVexBooks.Models.Entities;

public partial class Usuarios
{
    public int IdUsuario { get; set; }

    public string Nombre { get; set; } = null!;

    public string Correo { get; set; } = null!;

    public string ContraseñaHash { get; set; } = null!;

    public DateTime FechaRegistro { get; set; }

    public virtual ICollection<Partelikes> Partelikes { get; set; } = new List<Partelikes>();
}
