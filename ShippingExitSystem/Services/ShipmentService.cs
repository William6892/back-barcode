using Microsoft.EntityFrameworkCore;
using ShippingExitSystem.Data;
using ShippingExitSystem.Models;
using ShippingExitSystem.DTOs;

namespace ShippingExitSystem.Services
{
    public class ShipmentService : IShipmentService
    {
        private readonly ApplicationDbContext _context;

        public ShipmentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Shipment> CreateShipmentAsync(CreateShipmentDto dto, int userId)
        {
            var existing = await _context.Shipments.FirstOrDefaultAsync(s => s.ShipmentNumber == dto.ShipmentNumber);
            if (existing != null)
                throw new InvalidOperationException($"El número de envío '{dto.ShipmentNumber}' ya existe. Usa uno diferente.");

            var shipment = new Shipment
            {
                ShipmentNumber = dto.ShipmentNumber,
                Carrier = dto.Carrier,
                DriverName = dto.DriverName,
                DriverDocument = dto.DriverDocument,
                VehiclePlate = dto.VehiclePlate,
                VehicleModel = dto.VehicleModel,
                ScheduledDate = dto.ScheduledDate.ToUniversalTime(),
                Status = "Active",
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Shipments.Add(shipment);
            await _context.SaveChangesAsync();

            foreach (var productDto in dto.ExpectedProducts)
            {
                var expectedProduct = new ExpectedProduct
                {
                    ShipmentId = shipment.Id,
                    Name = productDto.Name,
                    Model = productDto.Model,
                    CreatedByUserId = userId
                };
                _context.ExpectedProducts.Add(expectedProduct);
            }

            await _context.SaveChangesAsync();
            return shipment;
        }

        public async Task<ExpectedProduct?> AddProductToShipmentAsync(int shipmentId, ExpectedProductDto dto, int userId)
        {
            var shipment = await _context.Shipments.FindAsync(shipmentId);
            if (shipment == null || shipment.Status != "Active")
                return null;

            var expectedProduct = new ExpectedProduct
            {
                ShipmentId = shipmentId,
                Name = dto.Name,
                Model = dto.Model,
                CreatedByUserId = userId
            };

            _context.ExpectedProducts.Add(expectedProduct);
            await _context.SaveChangesAsync();
            return expectedProduct;
        }

        public async Task<bool> ScanProductAsync(int shipmentId, ScanProductDto dto, int userId)
        {
            var shipment = await _context.Shipments.FindAsync(shipmentId);
            if (shipment == null || shipment.Status != "Active")
                return false;

            var expectedProduct = await _context.ExpectedProducts
                .FirstOrDefaultAsync(ep => ep.Id == dto.ExpectedProductId && ep.ShipmentId == shipmentId);

            if (expectedProduct == null)
                return false;

            // Buscar duplicado global en ScannedItems
            var duplicate = await _context.ScannedItems
                .Include(si => si.ScannedByUser)
                .Include(si => si.ExpectedProduct)
                    .ThenInclude(ep => ep.Shipment)
                .FirstOrDefaultAsync(si => si.Barcode == dto.Barcode);

            if (duplicate != null)
            {
                var shipmentNumber = duplicate.ExpectedProduct.Shipment.ShipmentNumber;
                var scannedBy = duplicate.ScannedByUser?.Username ?? "Desconocido";
                var scannedAt = duplicate.ScannedAt.ToString("dd/MM/yyyy HH:mm");
                throw new InvalidOperationException($"El serial '{dto.Barcode}' ya fue escaneado en el envío '{shipmentNumber}' por el usuario '{scannedBy}' el {scannedAt}.");
            }

            var scannedItem = new ScannedItem
            {
                ExpectedProductId = dto.ExpectedProductId,
                Barcode = dto.Barcode,
                ScannedAt = DateTime.UtcNow,
                ScannedByUserId = userId
            };

            _context.ScannedItems.Add(scannedItem);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<ShipmentResponseDto> GetShipmentAsync(int id)
        {
            var shipment = await _context.Shipments
                .Include(s => s.CreatedByUser)
                .Include(s => s.ExpectedProducts)
                    .ThenInclude(ep => ep.CreatedByUser)
                .Include(s => s.ExpectedProducts)
                    .ThenInclude(ep => ep.ScannedItems)
                        .ThenInclude(si => si.ScannedByUser)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (shipment == null) return null;

            var products = shipment.ExpectedProducts.Select(ep => new ProductSummaryDto
            {
                ExpectedProductId = ep.Id,
                Name = ep.Name,
                Model = ep.Model,
                ScannedCount = ep.ScannedItems.Count,
                CreatedByUserName = ep.CreatedByUser?.Username ?? "Desconocido",
                ScannedItems = ep.ScannedItems.Select(si => new ScannedItemDetailDto
                {
                    Barcode = si.Barcode,
                    ScannedAt = si.ScannedAt,
                    ScannedByUserName = si.ScannedByUser?.Username ?? "Desconocido"
                }).ToList()
            }).ToList();

            return new ShipmentResponseDto
            {
                Id = shipment.Id,
                ShipmentNumber = shipment.ShipmentNumber,
                Carrier = shipment.Carrier,
                DriverName = shipment.DriverName,
                VehiclePlate = shipment.VehiclePlate,
                Status = shipment.Status,
                CreatedAt = shipment.CreatedAt,
                CreatedByUserName = shipment.CreatedByUser?.Username ?? "Desconocido",
                Products = products
            };
        }

        public async Task<List<ShipmentResponseDto>> GetAllShipmentsAsync(string? status = null)
        {
            var query = _context.Shipments
                .Include(s => s.CreatedByUser)
                .Include(s => s.ExpectedProducts)
                    .ThenInclude(ep => ep.CreatedByUser)
                .Include(s => s.ExpectedProducts)
                    .ThenInclude(ep => ep.ScannedItems)
                        .ThenInclude(si => si.ScannedByUser)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(s => s.Status == status);

            var shipments = await query.ToListAsync();

            return shipments.Select(shipment => new ShipmentResponseDto
            {
                Id = shipment.Id,
                ShipmentNumber = shipment.ShipmentNumber,
                Carrier = shipment.Carrier,
                DriverName = shipment.DriverName,
                VehiclePlate = shipment.VehiclePlate,
                Status = shipment.Status,
                CreatedAt = shipment.CreatedAt,
                CreatedByUserName = shipment.CreatedByUser?.Username ?? "Desconocido",
                Products = shipment.ExpectedProducts.Select(ep => new ProductSummaryDto
                {
                    ExpectedProductId = ep.Id,
                    Name = ep.Name,
                    Model = ep.Model,
                    ScannedCount = ep.ScannedItems.Count,
                    CreatedByUserName = ep.CreatedByUser?.Username ?? "Desconocido",
                    ScannedItems = ep.ScannedItems.Select(si => new ScannedItemDetailDto
                    {
                        Barcode = si.Barcode,
                        ScannedAt = si.ScannedAt,
                        ScannedByUserName = si.ScannedByUser?.Username ?? "Desconocido"
                    }).ToList()
                }).ToList()
            }).ToList();
        }

        public async Task<bool> CompleteShipmentAsync(int shipmentId, int userId)
        {
            var shipment = await _context.Shipments.FindAsync(shipmentId);
            if (shipment == null || shipment.Status != "Active")
                return false;

            shipment.Status = "Completed";
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<BarcodeSearchResponseDto?> SearchBarcodeAsync(string barcode)
        {
            var scannedItem = await _context.ScannedItems
                .Include(si => si.ScannedByUser)
                .Include(si => si.ExpectedProduct)
                    .ThenInclude(ep => ep.Shipment)
                        .ThenInclude(s => s.CreatedByUser)
                .FirstOrDefaultAsync(si => si.Barcode == barcode);

            if (scannedItem == null) return null;

            var ep = scannedItem.ExpectedProduct;
            var shipment = ep.Shipment;

            return new BarcodeSearchResponseDto
            {
                Barcode = scannedItem.Barcode,
                ScannedAt = scannedItem.ScannedAt,
                ScannedByUserName = scannedItem.ScannedByUser?.Username ?? "Desconocido",
                ShipmentId = shipment.Id,
                ShipmentNumber = shipment.ShipmentNumber,
                Status = shipment.Status,
                Carrier = shipment.Carrier,
                DriverName = shipment.DriverName,
                DriverDocument = shipment.DriverDocument,
                VehiclePlate = shipment.VehiclePlate,
                ProductName = ep.Name,
                ProductModel = ep.Model
            };
        }

        public async Task<List<ShipmentResponseDto>> SearchShipmentsAsync(string? shipmentNumber, string? vehiclePlate)
        {
            var query = _context.Shipments
                .Include(s => s.CreatedByUser)
                .Include(s => s.ExpectedProducts)
                    .ThenInclude(ep => ep.CreatedByUser)
                .Include(s => s.ExpectedProducts)
                    .ThenInclude(ep => ep.ScannedItems)
                        .ThenInclude(si => si.ScannedByUser)
                .AsQueryable();

            if (!string.IsNullOrEmpty(shipmentNumber))
            {
                query = query.Where(s => s.ShipmentNumber.Contains(shipmentNumber));
            }

            if (!string.IsNullOrEmpty(vehiclePlate))
            {
                query = query.Where(s => s.VehiclePlate != null && s.VehiclePlate.Contains(vehiclePlate));
            }

            var shipments = await query.ToListAsync();

            return shipments.Select(shipment => new ShipmentResponseDto
            {
                Id = shipment.Id,
                ShipmentNumber = shipment.ShipmentNumber,
                Carrier = shipment.Carrier,
                DriverName = shipment.DriverName,
                VehiclePlate = shipment.VehiclePlate,
                Status = shipment.Status,
                CreatedAt = shipment.CreatedAt,
                CreatedByUserName = shipment.CreatedByUser?.Username ?? "Desconocido",
                Products = shipment.ExpectedProducts.Select(ep => new ProductSummaryDto
                {
                    ExpectedProductId = ep.Id,
                    Name = ep.Name,
                    Model = ep.Model,
                    ScannedCount = ep.ScannedItems.Count,
                    CreatedByUserName = ep.CreatedByUser?.Username ?? "Desconocido",
                    ScannedItems = ep.ScannedItems.Select(si => new ScannedItemDetailDto
                    {
                        Barcode = si.Barcode,
                        ScannedAt = si.ScannedAt,
                        ScannedByUserName = si.ScannedByUser?.Username ?? "Desconocido"
                    }).ToList()
                }).ToList()
            }).ToList();
        }

        public async Task<DashboardStatsDto> GetDashboardStatsAsync()
        {
            var today = DateTime.UtcNow.Date;

            var activeCount = await _context.Shipments.CountAsync(s => s.Status == "Active");
            var completedCount = await _context.Shipments.CountAsync(s => s.Status == "Completed");

            var totalToday = await _context.Shipments.CountAsync(s => s.CreatedAt >= today);
            var scannedToday = await _context.ScannedItems.CountAsync(si => si.ScannedAt >= today);

            var topCarriers = await _context.Shipments
                .Where(s => !string.IsNullOrEmpty(s.Carrier))
                .GroupBy(s => s.Carrier)
                .Select(g => new DashboardTopItemDto
                {
                    Name = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();

            var topProducts = await _context.ExpectedProducts
                .GroupBy(ep => ep.Name)
                .Select(g => new DashboardTopItemDto
                {
                    Name = g.Key,
                    Count = g.Sum(x => x.ScannedItems.Count)
                })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();

            return new DashboardStatsDto
            {
                ActiveShipmentsCount = activeCount,
                CompletedShipmentsCount = completedCount,
                TotalShipmentsToday = totalToday,
                TotalProductsScannedToday = scannedToday,
                TopCarriers = topCarriers,
                TopProducts = topProducts
            };
        }
    }
}