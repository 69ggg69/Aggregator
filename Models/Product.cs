namespace Aggregator.Models
{
    public class Product
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Price { get; set; }
        public required string Shop { get; set; }
        public DateTime ParseDate { get; set; }
        public string? ImageUrl { get; set; }
        public string? LocalImagePath { get; set; }
    }
}