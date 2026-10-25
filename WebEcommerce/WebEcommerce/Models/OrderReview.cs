
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebEcommerce.Models
{
    [Table("OrderReviews", Schema = "public")]
    public class OrderReview
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("OrderReviewID")]
        public int OrderReviewID { get; set; }

        [Required]
        [Column("OrderID")]
        public int OrderID { get; set; }

        [Required]
        [StringLength(128)]
        [Column("UserID")]
        public string UserID { get; set; }

        [Required]
        [Range(1, 5)]
        [Column("Rating")]
        public int Rating { get; set; }

        [Column("Comment")]
        public string Comment { get; set; }

        [Required]
        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [Column("IsApproved")]
        public bool IsApproved { get; set; } = true;

        [ForeignKey("OrderID")]
        public virtual Order Order { get; set; }

        [ForeignKey("UserID")]
        public virtual ApplicationUser User { get; set; }
    }
}
