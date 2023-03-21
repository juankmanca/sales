using System;
using System.Collections.Generic;

namespace TEST.Models;

public partial class Usuario
{
    public int Id { get; set; }

    public string? Nombre { get; set; }

    public DateTime? Fecha { get; set; }

    public string? Clave { get; set; }
}
