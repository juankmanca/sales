using System.ComponentModel.DataAnnotations;

namespace Sales.Share.entities
{
    public class City
    {
        public int Id { get; set; }

        [Required(ErrorMessage = " El campo {0} es obligatorio")]
        [MaxLength(100, ErrorMessage = "El campo {0} no puede tener mas de {1} caractéres ")]
        [Display(Name = "Ciudad")]
        public string Name { get; set; } = null!;

        public int StateId { get; set; }

        public State? State { get; set; }
    }
}
