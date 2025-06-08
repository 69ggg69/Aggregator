using System.ComponentModel.DataAnnotations;

namespace Aggregator.Models
{
    public class Tag
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;
        
        // Navigation properties
        public ICollection<ProductTag> ProductTags { get; set; } = [];
        
        // Convenience navigation property
        public ICollection<Product> Products => ProductTags.Select(pt => pt.Product).ToList();
    }
} 