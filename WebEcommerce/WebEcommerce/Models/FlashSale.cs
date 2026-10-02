using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebEcommerce.Models
{
    [Table("FlashSales", Schema = "public")]
    public class FlashSale
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("FlashSaleID")]
        public int FlashSaleID { get; set; }

        [Required]
        [StringLength(200)]
        [Column("Name")]
        public string Name { get; set; }

        [Required]
        [Column("StartTime")]
        public DateTime StartTime { get; set; }

        [Required]
        [Column("EndTime")]
        public DateTime EndTime { get; set; }

        [Column("IsActive")]
        public bool IsActive { get; set; } = true;

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual ICollection<FlashSaleItem> FlashSaleItems { get; set; } = new HashSet<FlashSaleItem>();
    }

    [Table("FlashSaleItems", Schema = "public")]
    public class FlashSaleItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("FlashSaleItemID")]
        public int FlashSaleItemID { get; set; }

        [Required]
        [Column("FlashSaleID")]
        public int FlashSaleID { get; set; }

        [Required]
        [Column("ProductID")]
        public int ProductID { get; set; }

        [Required]
        [Column("FlashSalePrice")]
        public decimal FlashSalePrice { get; set; }

        [Required]
        [Column("StockQuantity")]
        public int StockQuantity { get; set; }

        [Column("SoldQuantity")]
        public int SoldQuantity { get; set; } = 0;

        // Navigation properties
        [ForeignKey("FlashSaleID")]
        public virtual FlashSale FlashSale { get; set; }

        [ForeignKey("ProductID")]
        public virtual Product Product { get; set; }
    }
}
