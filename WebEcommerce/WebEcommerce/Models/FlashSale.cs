using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebEcommerce.Models
{
    /// <summary>
    /// Bảng Flash Sale - Quản lý các đợt giảm giá chớp nhoáng
    /// </summary>
    [Table("FlashSales", Schema = "public")]
    public class FlashSale
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("FlashSaleID")]
        public int FlashSaleID { get; set; }

        [Required]
        [StringLength(200)]
        [Column("Title")]
        public string Title { get; set; }

        [Column("Description")]
        public string Description { get; set; }

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

        // Navigation
        public virtual ICollection<FlashSaleItem> FlashSaleItems { get; set; } = new HashSet<FlashSaleItem>();
    }

    /// <summary>
    /// Bảng Flash Sale Item - Sản phẩm tham gia Flash Sale
    /// </summary>
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

        [Column("MaxQuantity")]
        public int MaxQuantity { get; set; } = 50;

        [Column("SoldQuantity")]
        public int SoldQuantity { get; set; } = 0;

        // Navigation
        [ForeignKey("FlashSaleID")]
        public virtual FlashSale FlashSale { get; set; }

        [ForeignKey("ProductID")]
        public virtual Product Product { get; set; }
    }
}
