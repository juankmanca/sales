using System;
using System.Collections.Generic;

namespace TEST2.Models;

public partial class Employee
{
    public int Id { get; set; }

    public string? Position { get; set; }

    public int? IdUser { get; set; }

    public virtual Usuario? IdUserNavigation { get; set; }
}
