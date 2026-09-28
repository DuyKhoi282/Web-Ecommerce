using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebEcommerce.Models
{
    [Table("Reviews", Schema = "public")]
    public class Review
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("ReviewID")]
        public int ReviewID { get; set; }

        [Required]
        [Column("ProductID")]
        public int ProductID { get; set; }

        [Required]
        [StringLength(128)]
        [Column("UserID")]
        public string UserID { get; set; }

        [Required]
        [Column("OrderID")]
        public int OrderID { get; set; }

        [Required]
        [Range(1, 5)]
        [Column("Rating")]
        public int Rating { get; set; }

        [Column("Comment")]
        public string Comment { get; set; }

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("IsApproved")]
        public bool IsApproved { get; set; } = true;

        // Navigation properties
        [ForeignKey("ProductID")]
        public virtual Product Product { get; set; }

        [ForeignKey("UserID")]
        public virtual ApplicationUser User { get; set; }

        [ForeignKey("OrderID")]
        public virtual Order Order { get; set; }
    }
}
