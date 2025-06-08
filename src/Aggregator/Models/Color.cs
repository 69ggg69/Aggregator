using System.ComponentModel.DataAnnotations;

namespace Aggregator.Models
{
    public class Color
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(7)]
        [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "HEX code must be in format #RRGGBB")]
        public string HexCode { get; set; } = string.Empty;
        
        // Navigation properties
        public ICollection<ProductVariant> ProductVariants { get; set; } = [];
    }
} 