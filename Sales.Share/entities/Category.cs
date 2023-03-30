using Sales.Share.entities;
using System.ComponentModel.DataAnnotations;

namespace Sales.Share.entities
{
    public class Category
    {
        public int Id { get; set; }

        [Required(ErrorMessage = " El campo {0} es obligatorio")]
        [MaxLength(100, ErrorMessage = "El campo {0} no puede tener mas de {1} caractéres ")]
        [Display(Name = "Categoria")]
        public string Name { get; set; } = null!;
        public ICollection<ProductCategory>? ProductCategories { get; set; }

        [Display(Name = "Productos")]
        public int ProductCategoriesNumber => ProductCategories == null ? 0 : ProductCategories.Count;

    }
}
