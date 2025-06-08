using System.ComponentModel.DataAnnotations;

namespace Aggregator.Models
{
    public class Size
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;
        
        // Navigation properties
        public ICollection<ProductVariant> ProductVariants { get; set; } = [];
    }
} 