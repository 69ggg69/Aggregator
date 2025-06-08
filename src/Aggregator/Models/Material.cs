using System.ComponentModel.DataAnnotations;

namespace Aggregator.Models
{
    public class Material
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;
        
        // Navigation properties
        public ICollection<Product> Products { get; set; } = [];
    }
} 