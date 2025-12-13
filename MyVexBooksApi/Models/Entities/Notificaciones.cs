using System;
using System.Collections.Generic;

namespace MyVexBooks.Models.Entities;

public partial class Notificaciones
{
    public int Id { get; set; }

    public string Endpoint { get; set; } = null!;

    public string P256dh { get; set; } = null!;

    public string Auth { get; set; } = null!;

    public string? UserAgent { get; set; }

    public DateTime? FechaCreacion { get; set; }

    public bool? Activo { get; set; }
}
