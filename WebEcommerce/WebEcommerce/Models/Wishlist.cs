using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebEcommerce.Models
{
    [Table("Wishlists", Schema = "public")]
    public class Wishlist
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("WishlistID")]
        public int WishlistID { get; set; }

        [Required]
        [StringLength(128)]
        [Column("UserID")]
        public string UserID { get; set; }

        [Required]
        [Column("ProductID")]
        public int ProductID { get; set; }

        [Column("AddedAt")]
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserID")]
        public virtual ApplicationUser User { get; set; }

        [ForeignKey("ProductID")]
        public virtual Product Product { get; set; }
    }
}
