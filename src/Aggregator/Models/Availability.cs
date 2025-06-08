using System.ComponentModel.DataAnnotations;

namespace Aggregator.Models
{
    public class Availability
    {
        public int Id { get; set; }
        
        [Required]
        public int VariantId { get; set; }
        
        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be non-negative")]
        public int Quantity { get; set; }
        
        public bool IsAvailable { get; set; }
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public ProductVariant Variant { get; set; } = null!;
    }
} 