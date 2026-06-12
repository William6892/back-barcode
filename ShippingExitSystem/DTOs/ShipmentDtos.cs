namespace ShippingExitSystem.DTOs
{
    public class CreateShipmentDto
    {
        public string ShipmentNumber { get; set; } = string.Empty;
        public string Carrier { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public string DriverDocument { get; set; } = string.Empty;
        public string VehiclePlate { get; set; } = string.Empty;
        public string VehicleModel { get; set; } = string.Empty;
        public DateTime ScheduledDate { get; set; }
        public List<ExpectedProductDto> ExpectedProducts { get; set; } = new();
    }

    public class ExpectedProductDto
    {
        public string Name { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
    }

    public class ScanProductDto
    {
        public int ExpectedProductId { get; set; }
        public string Barcode { get; set; } = string.Empty;
    }

    public class ShipmentResponseDto
    {
        public int Id { get; set; }
        public string ShipmentNumber { get; set; } = string.Empty;
        public string Carrier { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
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