using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebEcommerce.Models
{
    [Table("OrderDetails", Schema = "public")]
    public class OrderDetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("OrderDetailID")]
        public int OrderDetailID { get; set; }

        [Required]
        [Column("OrderID")]
        public int OrderID { get; set; }

        [Required]
        [Column("ProductID")]
        public int ProductID { get; set; }

        [Required]
        [Column("Quantity")]
        public int Quantity { get; set; } = 1;

        [Required]
        [Column("UnitPrice")]
        public decimal UnitPrice { get; set; }

        [Required]
        [Column("Subtotal")]
        public decimal Subtotal { get; set; }

        // Navigation properties
        [ForeignKey("OrderID")]
        public virtual Order Order { get; set; }

        [ForeignKey("ProductID")]
        public virtual Product Product { get; set; }
    }
}
