using System.ComponentModel.DataAnnotations;

namespace Aggregator.Models
{
    public class Image
    {
        public int Id { get; set; }
        
        [Required]
        public int VariantId { get; set; }
        
        [Required]
        [MaxLength(500)]
        public string Path { get; set; } = string.Empty;
        
        [MaxLength(255)]
        public string? AltText { get; set; }
        
        public int SortOrder { get; set; } = 0;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public ProductVariant Variant { get; set; } = null!;
        public ICollection<ProductVariant> ProductVariantsAsPicture { get; set; } = [];
    }
} 