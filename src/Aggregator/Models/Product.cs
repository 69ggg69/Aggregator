using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aggregator.Models
{
    public class Product
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;
        
        [Column(TypeName = "text")]
        public string? Description { get; set; }
        
        [Required]
        public ProductAudience Audience { get; set; }
        
        [Required]
        public int MaterialId { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Material Material { get; set; } = null!;
        public ICollection<ProductCategory> ProductCategories { get; set; } = [];
        public ICollection<ProductTag> ProductTags { get; set; } = [];
        public ICollection<ProductVariant> ProductVariants { get; set; } = [];
        
        // Convenience navigation properties
        public ICollection<Category> Categories => ProductCategories.Select(pc => pc.Category).ToList();
        public ICollection<Tag> Tags => ProductTags.Select(pt => pt.Tag).ToList();
    }

    public enum ProductAudience
    {
        Male,
        Female,
        Kids,
        Unisex
    }
}