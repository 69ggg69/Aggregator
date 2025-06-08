using System.ComponentModel.DataAnnotations;

namespace Aggregator.Models
{
    public class Category
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;
        
        public int? ParentId { get; set; }
        
        // Navigation properties
        public Category? Parent { get; set; }
        public ICollection<Category> Children { get; set; } = [];
        public ICollection<ProductCategory> ProductCategories { get; set; } = [];
        
        // Convenience navigation property
        public ICollection<Product> Products => ProductCategories.Select(pc => pc.Product).ToList();
    }
} 