using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebEcommerce.Models
{
    [Table("CartItems", Schema = "public")]
    public class CartItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("CartItemID")]
        public int CartItemID { get; set; }

        [Required]
        [Column("CartID")]
        public int CartID { get; set; }

        [Required]
        [Column("ProductID")]
        public int ProductID { get; set; }

        [Required]
        [Column("Quantity")]
        public int Quantity { get; set; } = 1;

        [Required]
        [Column("UnitPriceAtAddition")]
        public decimal UnitPriceAtAddition { get; set; }

        // Navigation properties
        [ForeignKey("CartID")]
        public virtual Cart Cart { get; set; }

        [ForeignKey("ProductID")]
        public virtual Product Product { get; set; }
    }
}
