﻿using System.ComponentModel.DataAnnotations;

namespace Sales.Share.entities
{
    public class Country
    {
        public int Id { get; set; }

        [Required(ErrorMessage = " El campo {0} es obligatorio")]
        [MaxLength(100, ErrorMessage = "El campo {0} no puede tener mas de {1} caractéres ")]
        [Display(Name = "País")]
        public string Name { get; set; } = null!;

        public ICollection<State>? States { get; set; }

        // Propiedad de lectura (No se mapea en la db)
        public int StatesNumber => States == null ? 0 : States.Count;
    }
}
