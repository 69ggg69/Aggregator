using System.ComponentModel.DataAnnotations;

namespace Aggregator.Models
{
    public class ProductTag
    {
        [Required]
        public int ProductId { get; set; }
        
        [Required]
        public int TagId { get; set; }
        
        // Navigation properties
        public Product Product { get; set; } = null!;
        public Tag Tag { get; set; } = null!;
    }
} 