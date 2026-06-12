using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShippingExitSystem.Models
{
    public class ExpectedProduct
    {
        [Key]
        public int Id { get; set; }

        public int ShipmentId { get; set; }
        [ForeignKey("ShipmentId")]
        public virtual Shipment Shipment { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        public string Model { get; set; } = string.Empty;

        public int? CreatedByUserId { get; set; }
        [ForeignKey("CreatedByUserId")]
        public virtual User? CreatedByUser { get; set; }

        public virtual ICollection<ScannedItem> ScannedItems { get; set; } = new List<ScannedItem>();
    }
}