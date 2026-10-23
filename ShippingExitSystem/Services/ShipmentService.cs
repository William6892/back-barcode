using Microsoft.EntityFrameworkCore;
using Npgsql;
using ShippingExitSystem.Data;
using ShippingExitSystem.Models;
using ShippingExitSystem.DTOs;

namespace ShippingExitSystem.Services
{
    public class ShipmentService : IShipmentService
    {
        public const int MaxPageSize = 100;

        private readonly ApplicationDbContext _context;

        public ShipmentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Shipment> CreateShipmentAsync(CreateShipmentDto dto, int userId)
        {
            var shipmentNumber = dto.ShipmentNumber.Trim();
            if (await _context.Shipments.AnyAsync(s => s.ShipmentNumber == shipmentNumber))
                throw new InvalidOperationException($"El número de envío '{shipmentNumber}' ya existe. Usa uno diferente.");

            var shipment = new Shipment
            {
                ShipmentNumber = shipmentNumber,
                Carrier = dto.Carrier,
                DriverName = dto.DriverName,
                DriverDocument = dto.DriverDocument,
                VehiclePlate = dto.VehiclePlate?.Trim().ToUpperInvariant() ?? string.Empty,
                VehicleModel = dto.VehicleModel,
                ScheduledDate = dto.ScheduledDate.ToUniversalTime(),
                Status = "Active",
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            foreach (var productDto in dto.ExpectedProducts)
            {
                shipment.ExpectedProducts.Add(new ExpectedProduct
                {
                    Name = productDto.Name,
                    Model = productDto.Model,
                    ExpectedQuantity = productDto.ExpectedQuantity,
                    CreatedByUserId = userId
                });
            }

            // Envío y referencias en una sola transacción
            _context.Shipments.Add(shipment);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                throw new InvalidOperationException($"El número de envío '{shipmentNumber}' ya existe. Usa uno diferente.");
            }
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
                ExpectedQuantity = dto.ExpectedQuantity,
                CreatedByUserId = userId
            };

            _context.ExpectedProducts.Add(expectedProduct);
            await _context.SaveChangesAsync();
            return expectedProduct;
        }

        public async Task<ScanResultDto?> ScanProductAsync(int shipmentId, ScanProductDto dto, int userId)
        {
            var barcode = dto.Barcode.Trim();
            if (barcode.Length == 0)
                throw new InvalidOperationException("El serial está vacío.");

            // Bloquea la fila de la referencia para que dos guardas no superen la cantidad esperada al mismo tiempo
            await using var transaction = _context.Database.IsRelational()
                ? await _context.Database.BeginTransactionAsync()
                : null;

            if (transaction != null)
            {
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT 1 FROM \"ExpectedProducts\" WHERE \"Id\" = {dto.ExpectedProductId} FOR UPDATE");
            }

            var product = await _context.ExpectedProducts
                .AsNoTracking()
                .Where(ep => ep.Id == dto.ExpectedProductId && ep.ShipmentId == shipmentId && ep.Shipment.Status == "Active")
                .Select(ep => new { ep.Id, ep.Name, ep.ExpectedQuantity, Scanned = ep.ScannedItems.Count() })
                .FirstOrDefaultAsync();

            if (product == null)
                return null;

            await ThrowIfDuplicateAsync(barcode);

            if (product.ExpectedQuantity > 0 && product.Scanned >= product.ExpectedQuantity)
                throw new InvalidOperationException(
                    $"La referencia '{product.Name}' ya tiene sus {product.ExpectedQuantity} unidades completas. No se pueden despachar más.");

            _context.ScannedItems.Add(new ScannedItem
            {
                ExpectedProductId = product.Id,
                Barcode = barcode,
                ScannedAt = DateTime.UtcNow,
                ScannedByUserId = userId
            });

            try
            {
                await _context.SaveChangesAsync();
                if (transaction != null)
                    await transaction.CommitAsync();
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                // Otro guarda escaneó el mismo serial en el mismo instante
                _context.ChangeTracker.Clear();
                if (transaction != null)
                    await transaction.RollbackAsync();
                await ThrowIfDuplicateAsync(barcode);
                throw new InvalidOperationException($"El serial '{barcode}' ya fue escaneado.");
            }

            return new ScanResultDto
            {
                Barcode = barcode,
                ExpectedProductId = product.Id,
                ScannedCount = product.Scanned + 1,
                ExpectedQuantity = product.ExpectedQuantity
            };
        }

        private async Task ThrowIfDuplicateAsync(string barcode)
        {
            var duplicate = await _context.ScannedItems
                .AsNoTracking()
                .Where(si => si.Barcode == barcode)
                .Select(si => new
                {
                    si.ExpectedProduct.Shipment.ShipmentNumber,
                    ScannedBy = si.ScannedByUser.Username,
                    si.ScannedAt
                })
                .FirstOrDefaultAsync();

            if (duplicate != null)
            {
                var scannedAt = duplicate.ScannedAt.ToString("dd/MM/yyyy HH:mm");
                throw new InvalidOperationException(
                    $"El serial '{barcode}' ya fue escaneado en el envío '{duplicate.ShipmentNumber}' por el usuario '{duplicate.ScannedBy ?? "Desconocido"}' el {scannedAt}.");
            }
        }

        private static bool IsUniqueViolation(DbUpdateException ex) =>
            ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

        public async Task<ShipmentResponseDto?> GetShipmentAsync(int id)
        {
            var shipment = await _context.Shipments
                .AsNoTracking()
                .Where(s => s.Id == id)
                .Select(s => new ShipmentResponseDto
                {
                    Id = s.Id,
                    ShipmentNumber = s.ShipmentNumber,
                    Carrier = s.Carrier,
                    DriverName = s.DriverName,
                    DriverDocument = s.DriverDocument,
                    VehiclePlate = s.VehiclePlate,
                    VehicleModel = s.VehicleModel,
                    ScheduledDate = s.ScheduledDate,
                    Status = s.Status,
                    CreatedAt = s.CreatedAt,
                    CreatedByUserName = s.CreatedByUser != null ? s.CreatedByUser.Username : "Desconocido",
                    TotalExpected = s.ExpectedProducts.Sum(ep => ep.ExpectedQuantity),
                    TotalScanned = s.ExpectedProducts.Sum(ep => ep.ScannedItems.Count()),
                    Products = s.ExpectedProducts
                        .OrderBy(ep => ep.Id)
                        .Select(ep => new ProductSummaryDto
                        {
                            ExpectedProductId = ep.Id,
                            Name = ep.Name,
                            Model = ep.Model,
                            ExpectedQuantity = ep.ExpectedQuantity,
                            ScannedCount = ep.ScannedItems.Count(),
                            CreatedByUserName = ep.CreatedByUser != null ? ep.CreatedByUser.Username : "Desconocido",
                            ScannedItems = ep.ScannedItems
                                .OrderBy(si => si.ScannedAt)
                                .Select(si => new ScannedItemDetailDto
                                {
                                    Barcode = si.Barcode,
                                    ScannedAt = si.ScannedAt,
                                    ScannedByUserName = si.ScannedByUser.Username
                                }).ToList()
                        }).ToList()
                })
                .AsSplitQuery()
                .FirstOrDefaultAsync();

            return shipment;
        }

        // Resumen sin seriales: las listas solo necesitan contadores, no las 1200 filas de cada envío
        private static IQueryable<ShipmentResponseDto> ProjectSummary(IQueryable<Shipment> query) =>
            query.Select(s => new ShipmentResponseDto
            {
                Id = s.Id,
                ShipmentNumber = s.ShipmentNumber,
                Carrier = s.Carrier,
                DriverName = s.DriverName,
                DriverDocument = s.DriverDocument,
                VehiclePlate = s.VehiclePlate,
                VehicleModel = s.VehicleModel,
                ScheduledDate = s.ScheduledDate,
                Status = s.Status,
                CreatedAt = s.CreatedAt,
                CreatedByUserName = s.CreatedByUser != null ? s.CreatedByUser.Username : "Desconocido",
                TotalExpected = s.ExpectedProducts.Sum(ep => ep.ExpectedQuantity),
                TotalScanned = s.ExpectedProducts.Sum(ep => ep.ScannedItems.Count()),
                Products = s.ExpectedProducts
                    .OrderBy(ep => ep.Id)
                    .Select(ep => new ProductSummaryDto
                    {
                        ExpectedProductId = ep.Id,
                        Name = ep.Name,
                        Model = ep.Model,
                        ExpectedQuantity = ep.ExpectedQuantity,
                        ScannedCount = ep.ScannedItems.Count(),
                        CreatedByUserName = ep.CreatedByUser != null ? ep.CreatedByUser.Username : "Desconocido"
                    }).ToList()
            });

        private static (int page, int pageSize) ClampPaging(int page, int pageSize) =>
            (Math.Max(1, page), Math.Clamp(pageSize, 1, MaxPageSize));

        public async Task<List<ShipmentResponseDto>> GetAllShipmentsAsync(string? status = null, int page = 1, int pageSize = 20)
        {
            (page, pageSize) = ClampPaging(page, pageSize);

            var query = _context.Shipments.AsNoTracking();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(s => s.Status == status);

            query = query
                .OrderByDescending(s => s.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

            return await ProjectSummary(query).ToListAsync();
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
            barcode = barcode.Trim();
            return await ProjectBarcode(_context.ScannedItems.AsNoTracking().Where(si => si.Barcode == barcode))
                .FirstOrDefaultAsync();
        }

        public async Task<List<BarcodeSearchResponseDto>> SearchBarcodesAsync(string query, int page = 1, int pageSize = 50)
        {
            (page, pageSize) = ClampPaging(page, pageSize);

            var items = _context.ScannedItems
                .AsNoTracking()
                .Where(si => si.Barcode.Contains(query))
                .OrderByDescending(si => si.ScannedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

            return await ProjectBarcode(items).ToListAsync();
        }

        private static IQueryable<BarcodeSearchResponseDto> ProjectBarcode(IQueryable<ScannedItem> query) =>
            query.Select(si => new BarcodeSearchResponseDto
            {
                Barcode = si.Barcode,
                ScannedAt = si.ScannedAt,
                ScannedByUserName = si.ScannedByUser.Username,
                ShipmentId = si.ExpectedProduct.Shipment.Id,
                ShipmentNumber = si.ExpectedProduct.Shipment.ShipmentNumber,
                Status = si.ExpectedProduct.Shipment.Status,
                Carrier = si.ExpectedProduct.Shipment.Carrier,
                DriverName = si.ExpectedProduct.Shipment.DriverName,
                DriverDocument = si.ExpectedProduct.Shipment.DriverDocument,
                VehiclePlate = si.ExpectedProduct.Shipment.VehiclePlate,
                ProductName = si.ExpectedProduct.Name,
                ProductModel = si.ExpectedProduct.Model
            });

        public async Task<List<ShipmentResponseDto>> SearchShipmentsAsync(string? shipmentNumber, string? vehiclePlate)
        {
            var query = _context.Shipments.AsNoTracking();

            if (!string.IsNullOrEmpty(shipmentNumber))
                query = query.Where(s => s.ShipmentNumber.Contains(shipmentNumber));

            if (!string.IsNullOrEmpty(vehiclePlate))
                query = query.Where(s => s.VehiclePlate != null && s.VehiclePlate.Contains(vehiclePlate));

            query = query.OrderByDescending(s => s.CreatedAt).Take(MaxPageSize);

            return await ProjectSummary(query).ToListAsync();
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
