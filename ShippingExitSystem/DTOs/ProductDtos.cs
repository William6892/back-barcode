using System.ComponentModel.DataAnnotations;

namespace BarcodeShippingSystem.DTOs
{
    public class CreateProductDto
    {
        public string? Barcode { get; set; }  // ← Código real de la pistola

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Model { get; set; }
    }

    public class ProductResponseDto
    {
        public int Id { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Model { get; set; }
        public bool IsAvailable { get; set; }
    }
}