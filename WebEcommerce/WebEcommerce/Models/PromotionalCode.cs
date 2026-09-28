using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebEcommerce.Models
{
    [Table("PromotionalCodes", Schema = "public")]
    public class PromotionalCode
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("PromoCodeID")]
        public int PromoCodeID { get; set; }

        [Required]
        [StringLength(50)]
        [Column("Code")]
        public string Code { get; set; }

        [Column("Description")]
        public string Description { get; set; }

        [Required]
        [StringLength(20)]
        [Column("DiscountType")]
        public string DiscountType { get; set; } = "Percentage"; // 'Percentage' or 'Fixed'

        [Required]
        [Column("DiscountValue")]
        public decimal DiscountValue { get; set; }

        [Column("MinOrderAmount")]
        public decimal MinOrderAmount { get; set; } = 0;

        [Column("MaxUsage")]
        public int MaxUsage { get; set; } = 100;

        [Column("CurrentUsage")]
        public int CurrentUsage { get; set; } = 0;

        [Required]
        [Column("StartDate")]
        public DateTime StartDate { get; set; }

        [Required]
        [Column("EndDate")]
        public DateTime EndDate { get; set; }

        [Column("IsActive")]
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<Order> Orders { get; set; } = new HashSet<Order>();
    }
}
