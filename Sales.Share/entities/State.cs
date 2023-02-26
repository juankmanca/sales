using System.ComponentModel.DataAnnotations;
namespace Sales.Share.entities
{
    public class State
    {
        public int Id { get; set; }

        [Required(ErrorMessage = " El campo {0} es obligatorio")]
        [MaxLength(100, ErrorMessage = "El campo {0} no puede tener mas de {1} caractéres ")]
        [Display(Name = "Estado/Departamento")]
        public string Name { get; set; } = null!;

        public int CountryId { get; set; }

        public Country? Country { get; set; }

        public ICollection<City>? Cities { get; set; }

        // Propiedad de lectura (No se mapea en la db)
        public int CitiesNumber => Cities == null ? 0 : Cities.Count;
    }
}
