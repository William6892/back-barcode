using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShippingExitSystem.Models
{
    public class Shipment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string ShipmentNumber { get; set; } = string.Empty;

        [StringLength(100)]
        public string Carrier { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string DriverName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string DriverDocument { get; set; } = string.Empty;

        [StringLength(20)]
        public string VehiclePlate { get; set; } = string.Empty;

        [StringLength(50)]
        public string VehicleModel { get; set; } = string.Empty;

        public DateTime ScheduledDate { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? CreatedByUserId { get; set; }
        [ForeignKey("CreatedByUserId")]
        public virtual User? CreatedByUser { get; set; }

        public virtual ICollection<ExpectedProduct> ExpectedProducts { get; set; } = new List<ExpectedProduct>();
    }
}