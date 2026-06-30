using ShippingExitSystem.Models;
using ShippingExitSystem.DTOs;

namespace ShippingExitSystem.Services
{
    public interface IShipmentService
    {
        Task<Shipment> CreateShipmentAsync(CreateShipmentDto dto, int userId);
        Task<ExpectedProduct?> AddProductToShipmentAsync(int shipmentId, ExpectedProductDto dto, int userId);
        Task<bool> ScanProductAsync(int shipmentId, ScanProductDto dto, int userId);
        Task<ShipmentResponseDto> GetShipmentAsync(int id);
        Task<List<ShipmentResponseDto>> GetAllShipmentsAsync(string? status = null, int page = 1, int pageSize = 20);
        Task<bool> CompleteShipmentAsync(int shipmentId, int userId);
        Task<BarcodeSearchResponseDto?> SearchBarcodeAsync(string barcode);
        Task<List<BarcodeSearchResponseDto>> SearchBarcodesAsync(string query, int page = 1, int pageSize = 50);
        Task<List<ShipmentResponseDto>> SearchShipmentsAsync(string? shipmentNumber, string? vehiclePlate);
        Task<DashboardStatsDto> GetDashboardStatsAsync();
    }
}