using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebEcommerce.Models
{
    [Table("Orders", Schema = "public")]
    public class Order
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("OrderID")]
        public int OrderID { get; set; }

        [StringLength(128)]
        [Column("UserID")]
        public string UserID { get; set; }

        [Column("PromoCodeID")]
        public int? PromoCodeID { get; set; }

        [Column("OrderDate")]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [Required]
        [Column("TotalAmount")]
        public decimal TotalAmount { get; set; }

        [Column("DiscountAmount")]
        public decimal DiscountAmount { get; set; } = 0;

        [Required]
        [Column("FinalAmount")]
        public decimal FinalAmount { get; set; }

        [Required]
        [StringLength(50)]
        [Column("Status")]
        public string Status { get; set; } = "Pending";

        [Required]
        [StringLength(300)]
        [Column("ShippingAddress")]
        public string ShippingAddress { get; set; }

        [Required]
        [StringLength(50)]
        [Column("ReceiverPhone")]
        public string ReceiverPhone { get; set; }

        [Required]
        [StringLength(50)]
        [Column("PaymentMethod")]
        public string PaymentMethod { get; set; } = "COD";

        [Required]
        [StringLength(50)]
        [Column("PaymentStatus")]
        public string PaymentStatus { get; set; } = "Unpaid";

        [Column("PaymentDate")]
        public DateTime? PaymentDate { get; set; }

        [Column("Notes")]
        public string Notes { get; set; }

        // Navigation properties
        [ForeignKey("UserID")]
        public virtual ApplicationUser User { get; set; }

        [ForeignKey("PromoCodeID")]
        public virtual PromotionalCode PromotionalCode { get; set; }

        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new HashSet<OrderDetail>();
        public virtual ICollection<Review> Reviews { get; set; } = new HashSet<Review>();
    }
}
