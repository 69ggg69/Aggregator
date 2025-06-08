using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aggregator.Models
{
    /// <summary>
    /// Статус парсинга товара
    /// </summary>
    public enum ParsingStatus
    {
        /// <summary>
        /// Товар не был спаршен
        /// </summary>
        NotParsed = 0,
        
        /// <summary>
        /// Спаршена только базовая информация (название, ссылка)
        /// </summary>
        BasicParsed = 1,
        
        /// <summary>
        /// Спаршена полная информация (описание, материал, варианты и т.д.)
        /// </summary>
        DetailedParsed = 2
    }

    /// <summary>
    /// Модель товара в системе агрегации
    /// Поддерживает двухэтапный парсинг: сначала базовая информация, потом детальная
    /// </summary>
    public class Product
    {
        /// <summary>
        /// Уникальный идентификатор товара
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Название товара (обязательное поле, заполняется на этапе базового парсинга)
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Описание товара (заполняется на этапе детального парсинга)
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Целевая аудитория товара (заполняется на этапе детального парсинга)
        /// </summary>
        public ProductAudience? Audience { get; set; }

        /// <summary>
        /// Ссылка на страницу товара для детального парсинга
        /// </summary>
        [MaxLength(1000)]
        public string? ProductUrl { get; set; }

        /// <summary>
        /// Статус парсинга товара
        /// </summary>
        public ParsingStatus ParsingStatus { get; set; } = ParsingStatus.NotParsed;

        // Связи с другими сущностями

        /// <summary>
        /// Идентификатор магазина
        /// </summary>
        [Required]
        public int ShopId { get; set; }

        /// <summary>
        /// Магазин, к которому относится товар
        /// </summary>
        [ForeignKey(nameof(ShopId))]
        public Shop Shop { get; set; } = null!;

        /// <summary>
        /// Идентификатор материала (заполняется на этапе детального парсинга)
        /// </summary>
        public string? MaterialId { get; set; }

        /// <summary>
        /// Материал товара (заполняется на этапе детального парсинга)
        /// </summary>
        [ForeignKey(nameof(MaterialId))]
        public Material? Material { get; set; }

        // Временные метки

        /// <summary>
        /// Дата создания записи
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Дата последнего обновления записи
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Навигационные свойства

        /// <summary>
        /// Варианты товара (цвет, размер, цена)
        /// </summary>
        public ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();

        /// <summary>
        /// Связи товара с категориями
        /// </summary>
        public ICollection<ProductCategory> ProductCategories { get; set; } = new List<ProductCategory>();

        /// <summary>
        /// Связи товара с тегами
        /// </summary>
        public ICollection<ProductTag> ProductTags { get; set; } = new List<ProductTag>();

        // Удобные свойства для доступа к связанным данным

        /// <summary>
        /// Категории товара
        /// </summary>
        [NotMapped]
        public IEnumerable<Category> Categories => ProductCategories.Select(pc => pc.Category);

        /// <summary>
        /// Теги товара
        /// </summary>
        [NotMapped]
        public IEnumerable<Tag> Tags => ProductTags.Select(pt => pt.Tag);
    }

    public enum ProductAudience
    {
        Male,
        Female,
        Kids,
        Unisex
    }
}