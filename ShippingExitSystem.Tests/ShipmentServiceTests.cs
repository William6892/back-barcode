using Microsoft.EntityFrameworkCore;
using ShippingExitSystem.Data;
using ShippingExitSystem.DTOs;
using ShippingExitSystem.Models;
using ShippingExitSystem.Services;
using Xunit;

namespace ShippingExitSystem.Tests
{
    public class ShipmentServiceTests
    {
        private ApplicationDbContext GetInMemoryDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            var context = new ApplicationDbContext(options);
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            return context;
        }

        [Fact]
        public async Task CreateShipmentAsync_ShouldCreateShipmentSuccessfully()
        {
            // Arrange
            var context = GetInMemoryDbContext("CreateShipment_Success");
            var service = new ShipmentService(context);
            var userId = 1;

            var dto = new CreateShipmentDto
            {
                ShipmentNumber = "ENV-100",
                Carrier = "Servientrega",
                DriverName = "Juan Perez",
                DriverDocument = "12345678",
                VehiclePlate = "ABC-123",
                VehicleModel = "Toyota Hilux",
                ScheduledDate = DateTime.UtcNow.AddDays(1),
                ExpectedProducts = new List<ExpectedProductDto>
                {
                    new ExpectedProductDto { Name = "Galaxy S24 Ultra", Model = "SM-S928B" }
                }
            };

            // Act
            var result = await service.CreateShipmentAsync(dto, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("ENV-100", result.ShipmentNumber);
            Assert.Equal("Active", result.Status);

            var dbShipment = await context.Shipments
                .Include(s => s.ExpectedProducts)
                .FirstOrDefaultAsync(s => s.Id == result.Id);

            Assert.NotNull(dbShipment);
            Assert.Single(dbShipment.ExpectedProducts);
            Assert.Equal("Galaxy S24 Ultra", dbShipment.ExpectedProducts.First().Name);
        }

        [Fact]
        public async Task CreateShipmentAsync_ShouldThrowException_WhenShipmentNumberExists()
        {
            // Arrange
            var context = GetInMemoryDbContext("CreateShipment_Duplicate");
            var service = new ShipmentService(context);
            var userId = 1;

            context.Shipments.Add(new Shipment
            {
                ShipmentNumber = "ENV-DUP",
                DriverName = "Pedro Gomez",
                DriverDocument = "87654321",
                ScheduledDate = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var dto = new CreateShipmentDto
            {
                ShipmentNumber = "ENV-DUP",
                DriverName = "Juan Perez",
                DriverDocument = "12345678"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CreateShipmentAsync(dto, userId));

            Assert.Contains("ya existe", exception.Message);
        }

        [Fact]
        public async Task ScanProductAsync_ShouldScanSuccessfully()
        {
            // Arrange
            var context = GetInMemoryDbContext("ScanProduct_Success");
            var service = new ShipmentService(context);
            var userId = 1;

            var shipment = new Shipment
            {
                ShipmentNumber = "ENV-SCAN",
                DriverName = "Driver",
                DriverDocument = "doc",
                Status = "Active",
                ScheduledDate = DateTime.UtcNow
            };
            context.Shipments.Add(shipment);
            await context.SaveChangesAsync();

            var product = new ExpectedProduct
            {
                ShipmentId = shipment.Id,
                Name = "Samsung Charger",
                Model = "EP-T4510"
            };
            context.ExpectedProducts.Add(product);
            await context.SaveChangesAsync();

            var dto = new ScanProductDto
            {
                ExpectedProductId = product.Id,
                Barcode = "8806090123456"
            };

            // Act
            var result = await service.ScanProductAsync(shipment.Id, dto, userId);

            // Assert
            Assert.True(result);
            var scanned = await context.ScannedItems.FirstOrDefaultAsync(si => si.Barcode == "8806090123456");
            Assert.NotNull(scanned);
            Assert.Equal(product.Id, scanned.ExpectedProductId);
        }

        [Fact]
        public async Task ScanProductAsync_ShouldThrow_WhenBarcodeIsGlobalDuplicate()
        {
            // Arrange
            var context = GetInMemoryDbContext("ScanProduct_Duplicate");
            var service = new ShipmentService(context);
            var userId = 1;

            var user = new User { Username = "inspector1", Email = "test@test.com", PasswordHash = "hash" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var shipment1 = new Shipment { ShipmentNumber = "ENV-1", DriverName = "D1", DriverDocument = "doc1", Status = "Active", ScheduledDate = DateTime.UtcNow };
            var shipment2 = new Shipment { ShipmentNumber = "ENV-2", DriverName = "D2", DriverDocument = "doc2", Status = "Active", ScheduledDate = DateTime.UtcNow };
            context.Shipments.AddRange(shipment1, shipment2);
            await context.SaveChangesAsync();

            var product1 = new ExpectedProduct { ShipmentId = shipment1.Id, Name = "Prod 1" };
            var product2 = new ExpectedProduct { ShipmentId = shipment2.Id, Name = "Prod 2" };
            context.ExpectedProducts.AddRange(product1, product2);
            await context.SaveChangesAsync();

            // Agregar serial escaneado en el envío 1
            context.ScannedItems.Add(new ScannedItem
            {
                ExpectedProductId = product1.Id,
                Barcode = "123456789",
                ScannedByUserId = user.Id,
                ScannedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var dto = new ScanProductDto
            {
                ExpectedProductId = product2.Id,
                Barcode = "123456789" // Intento de duplicar
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ScanProductAsync(shipment2.Id, dto, userId));

            Assert.Contains("ya fue escaneado", exception.Message);
            Assert.Contains("ENV-1", exception.Message);
        }
    }
}
