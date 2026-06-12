using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShippingExitSystem.Models
{
    public class ScannedItem
    {
        [Key]
        public int Id { get; set; }

        public int ExpectedProductId { get; set; }
        [ForeignKey("ExpectedProductId")]
        public virtual ExpectedProduct ExpectedProduct { get; set; }

        [Required]
        [StringLength(100)]
        public string Barcode { get; set; } = string.Empty;

        public DateTime ScannedAt { get; set; } = DateTime.UtcNow;

        public int ScannedByUserId { get; set; }
        [ForeignKey("ScannedByUserId")]
        public virtual User ScannedByUser { get; set; }
    }
}