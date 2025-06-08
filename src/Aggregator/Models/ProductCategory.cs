using System.ComponentModel.DataAnnotations;

namespace Aggregator.Models
{
    public class ProductCategory
    {
        [Required]
        public int ProductId { get; set; }
        
        [Required]
        public int CategoryId { get; set; }
        
        // Navigation properties
        public Product Product { get; set; } = null!;
        public Category Category { get; set; } = null!;
    }
} 