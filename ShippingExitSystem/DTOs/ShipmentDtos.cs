using System.ComponentModel.DataAnnotations;

namespace ShippingExitSystem.DTOs
{
    public class CreateShipmentDto
    {
        [Required(ErrorMessage = "El número de envío es obligatorio.")]
        [StringLength(50, ErrorMessage = "El número de envío no puede superar los 50 caracteres.")]
        public string ShipmentNumber { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "La transportadora no puede superar los 100 caracteres.")]
        public string Carrier { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre del conductor es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre del conductor no puede superar los 100 caracteres.")]
        public string DriverName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El documento del conductor es obligatorio.")]
        [StringLength(50, ErrorMessage = "El documento del conductor no puede superar los 50 caracteres.")]
        public string DriverDocument { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "La placa del vehículo no puede superar los 20 caracteres.")]
        public string VehiclePlate { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "El modelo del vehículo no puede superar los 50 caracteres.")]
        public string VehicleModel { get; set; } = string.Empty;

        [Required(ErrorMessage = "La fecha programada es obligatoria.")]
        public DateTime ScheduledDate { get; set; }

        public List<ExpectedProductDto> ExpectedProducts { get; set; } = new();
    }

    public class ExpectedProductDto
    {
        [Required(ErrorMessage = "El nombre del producto es obligatorio.")]
        [StringLength(200, ErrorMessage = "El nombre del producto no puede superar los 200 caracteres.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "El modelo del producto no puede superar los 100 caracteres.")]
        public string Model { get; set; } = string.Empty;
    }

    public class ScanProductDto
    {
        [Required(ErrorMessage = "El ID del producto esperado es obligatorio.")]
        public int ExpectedProductId { get; set; }

        [Required(ErrorMessage = "El código de barras es obligatorio.")]
        [StringLength(100, ErrorMessage = "El código de barras no puede superar los 100 caracteres.")]
        public string Barcode { get; set; } = string.Empty;
    }

    public class ShipmentResponseDto
    {
        public int Id { get; set; }
        public string ShipmentNumber { get; set; } = string.Empty;
        public string Carrier { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string VehiclePlate { get; set; } = string.Empty;    
        public DateTime CreatedAt { get; set; }                     
        public string CreatedByUserName { get; set; } = string.Empty;
        public List<ProductSummaryDto> Products { get; set; } = new();
    }

    public class ProductSummaryDto
    {
        public int ExpectedProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int ScannedCount { get; set; }
        public string CreatedByUserName { get; set; } = string.Empty;
        public List<ScannedItemDetailDto> ScannedItems { get; set; } = new();
    }

    public class ScannedItemDetailDto
    {
        public string Barcode { get; set; } = string.Empty;
        public DateTime ScannedAt { get; set; }
        public string ScannedByUserName { get; set; } = string.Empty;
    }

    public class BarcodeSearchResponseDto
    {
        public string Barcode { get; set; } = string.Empty;
        public DateTime ScannedAt { get; set; }
        public string ScannedByUserName { get; set; } = string.Empty;
        public int ShipmentId { get; set; }
        public string ShipmentNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Carrier { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public string DriverDocument { get; set; } = string.Empty;
        public string VehiclePlate { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ProductModel { get; set; } = string.Empty;
    }

    public class DashboardStatsDto
    {
        public int ActiveShipmentsCount { get; set; }
        public int CompletedShipmentsCount { get; set; }
        public int TotalShipmentsToday { get; set; }
        public int TotalProductsScannedToday { get; set; }
        public List<DashboardTopItemDto> TopCarriers { get; set; } = new();
        public List<DashboardTopItemDto> TopProducts { get; set; } = new();
    }

    public class DashboardTopItemDto
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}