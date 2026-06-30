using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShippingExitSystem.Services;
using ShippingExitSystem.DTOs;

namespace ShippingExitSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ShipmentController : ControllerBase
{
    private readonly IShipmentService _shipmentService;

    public ShipmentController(IShipmentService shipmentService)
    {
        _shipmentService = shipmentService;
    }

    private int GetUserId()
    {
        return int.Parse(User.FindFirst("userId")?.Value ?? "0");
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateShipmentDto dto)
    {
        try
        {
            var userId = GetUserId();
            var shipment = await _shipmentService.CreateShipmentAsync(dto, userId);
            return Ok(new { id = shipment.Id, shipmentNumber = shipment.ShipmentNumber });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/products")]
    public async Task<IActionResult> AddProduct(int id, ExpectedProductDto dto)
    {
        var userId = GetUserId();
        var product = await _shipmentService.AddProductToShipmentAsync(id, dto, userId);
        if (product == null)
            return BadRequest(new { error = "No se pudo añadir el producto. Verifique que el envío exista y esté activo." });
        return Ok(new { id = product.Id, name = product.Name, model = product.Model });
    }

    [HttpPost("{id}/scan")]
    public async Task<IActionResult> Scan(int id, ScanProductDto dto)
    {
        try
        {
            var userId = GetUserId();
            var result = await _shipmentService.ScanProductAsync(id, dto, userId);
            if (!result)
                return BadRequest(new { error = "Producto no encontrado o envío no activo" });
            return Ok(new { message = "Producto escaneado correctamente" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string barcode)
    {
        var result = await _shipmentService.SearchBarcodeAsync(barcode);
        if (result == null)
            return NotFound(new { error = "Código de barras no encontrado en ningún envío" });
        return Ok(result);
    }

    [HttpGet("search-shipments")]
    public async Task<IActionResult> SearchShipments([FromQuery] string? shipmentNumber, [FromQuery] string? vehiclePlate)
    {
        if (string.IsNullOrEmpty(shipmentNumber) && string.IsNullOrEmpty(vehiclePlate))
        {
            return BadRequest(new { error = "Debe proporcionar el número de envío (shipmentNumber) o la placa (vehiclePlate) para buscar." });
        }

        var results = await _shipmentService.SearchShipmentsAsync(shipmentNumber, vehiclePlate);
        return Ok(results);
    }

    [HttpGet("search-barcodes")]
    public async Task<IActionResult> SearchBarcodes([FromQuery] string query, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 3)
            return BadRequest(new { error = "La búsqueda debe tener al menos 3 caracteres." });

        var results = await _shipmentService.SearchBarcodesAsync(query.Trim(), page, pageSize);
        return Ok(results);
    }

    [HttpGet("dashboard/stats")]
    public async Task<IActionResult> GetStats()
    {
        var stats = await _shipmentService.GetDashboardStatsAsync();
        return Ok(stats);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var shipment = await _shipmentService.GetShipmentAsync(id);
        if (shipment == null)
            return NotFound();
        return Ok(shipment);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var shipments = await _shipmentService.GetAllShipmentsAsync(status, page, pageSize);
        return Ok(shipments);
    }

    [HttpPost("{id}/complete")]
    public async Task<IActionResult> Complete(int id)
    {
        var userId = GetUserId();
        var result = await _shipmentService.CompleteShipmentAsync(id, userId);
        if (!result)
            return BadRequest(new { error = "No se pudo completar el envío" });
        return Ok(new { message = "Envío completado" });
    }
}