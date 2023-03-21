using System;
using System.Collections.Generic;

namespace TEST2.Models;

public partial class Usuario
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Age { get; set; }

    public virtual ICollection<Employee> Employees { get; } = new List<Employee>();
}
