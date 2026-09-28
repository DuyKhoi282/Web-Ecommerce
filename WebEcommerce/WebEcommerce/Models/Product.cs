using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebEcommerce.Models
{
    [Table("Products", Schema = "public")]
    public class Product
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("ProductID")]
        public int ProductID { get; set; }

        [Required]
        [Column("CategoryID")]
        public int CategoryID { get; set; }

        [Required]
        [StringLength(255)]
        [Column("Name")]
        public string Name { get; set; }

        [Column("Description")]
        public string Description { get; set; }

        [Required]
        [Column("Price")]
        public decimal Price { get; set; }

        [Column("DiscountPrice")]
        public decimal? DiscountPrice { get; set; }

        [Column("StockQuantity")]
        public int StockQuantity { get; set; } = 0;

        [Column("Status")]
        public int Status { get; set; } = 1; // 1: In stock, 2: Out of stock, 3: Discontinued

        [Column("ViewCount")]
        public int ViewCount { get; set; } = 0;

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("CategoryID")]
        public virtual Category Category { get; set; }

        public virtual ICollection<ProductImage> ProductImages { get; set; } = new HashSet<ProductImage>();
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new HashSet<OrderDetail>();
        public virtual ICollection<CartItem> CartItems { get; set; } = new HashSet<CartItem>();
        public virtual ICollection<Review> Reviews { get; set; } = new HashSet<Review>();
        public virtual ICollection<Wishlist> Wishlists { get; set; } = new HashSet<Wishlist>();
    }
}
