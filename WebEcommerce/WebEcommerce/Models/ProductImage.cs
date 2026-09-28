using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebEcommerce.Models
{
    [Table("ProductImages", Schema = "public")]
    public class ProductImage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("ImageID")]
        public int ImageID { get; set; }

        [Required]
        [Column("ProductID")]
        public int ProductID { get; set; }

        [Required]
        [StringLength(300)]
        [Column("ImageURL")]
        public string ImageURL { get; set; }

        [Column("IsMain")]
        public bool IsMain { get; set; } = false;

        [Column("DisplayOrder")]
        public int DisplayOrder { get; set; } = 0;

        // Navigation properties
        [ForeignKey("ProductID")]
        public virtual Product Product { get; set; }
    }
}
