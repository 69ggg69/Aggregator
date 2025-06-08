using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aggregator.Models
{
    public class ProductVariant
    {
        public int Id { get; set; }
        
        [Required]
        public int ProductId { get; set; }
        
        [Required]
        public int ColorId { get; set; }
        
        [Required]
        public int SizeId { get; set; }
        
        public int? PictureId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Sku { get; set; } = string.Empty;
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public Product Product { get; set; } = null!;
        public Color Color { get; set; } = null!;
        public Size Size { get; set; } = null!;
        public Image? Picture { get; set; }
        public ICollection<Image> Images { get; set; } = [];
        public ICollection<Availability> Availabilities { get; set; } = [];
    }
} 