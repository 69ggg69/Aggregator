using Microsoft.EntityFrameworkCore;
using Aggregator.Models;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Aggregator.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets for all entities
        public DbSet<Product> Products { get; set; }
        public DbSet<Shop> Shops { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Material> Materials { get; set; }
        public DbSet<Color> Colors { get; set; }
        public DbSet<Size> Sizes { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<Image> Images { get; set; }
        public DbSet<Availability> Availabilities { get; set; }
        public DbSet<ProductCategory> ProductCategories { get; set; }
        public DbSet<ProductTag> ProductTags { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Shop entity
            ConfigureShop(modelBuilder);
            
            // Configure Product entity
            ConfigureProduct(modelBuilder);
            
            // Configure Category entity (hierarchical)
            ConfigureCategory(modelBuilder);
            
            // Configure ProductVariant entity
            ConfigureProductVariant(modelBuilder);
            
            // Configure Image entity
            ConfigureImage(modelBuilder);
            
            // Configure Availability entity
            ConfigureAvailability(modelBuilder);
            
            // Configure junction tables
            ConfigureProductCategory(modelBuilder);
            ConfigureProductTag(modelBuilder);
            
            // Configure indexes for performance
            ConfigureIndexes(modelBuilder);
        }

        private void ConfigureShop(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Shop>()
                .HasIndex(s => s.Name)
                .IsUnique();
            
            modelBuilder.Entity<Shop>()
                .HasIndex(s => s.Url)
                .IsUnique();
        }

        private void ConfigureProduct(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Shop)
                .WithMany(s => s.Products)
                .HasForeignKey(p => p.ShopId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Material)
                .WithMany(m => m.Products)
                .HasForeignKey(p => p.MaterialId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
                .Property(p => p.Audience)
                .HasConversion<string>();

            modelBuilder.Entity<Product>()
                .Property(p => p.ParsingStatus)
                .HasConversion<string>();

            modelBuilder.Entity<Product>()
                .Property(p => p.ProductUrl)
                .HasMaxLength(1000);
        }

        private void ConfigureCategory(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>()
                .HasOne(c => c.Parent)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Category>()
                .HasIndex(c => c.Name)
                .IsUnique();
        }

        private void ConfigureProductVariant(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProductVariant>()
                .HasOne(pv => pv.Product)
                .WithMany(p => p.ProductVariants)
                .HasForeignKey(pv => pv.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductVariant>()
                .HasOne(pv => pv.Color)
                .WithMany(c => c.ProductVariants)
                .HasForeignKey(pv => pv.ColorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductVariant>()
                .HasOne(pv => pv.Size)
                .WithMany(s => s.ProductVariants)
                .HasForeignKey(pv => pv.SizeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductVariant>()
                .HasOne(pv => pv.Picture)
                .WithMany(i => i.ProductVariantsAsPicture)
                .HasForeignKey(pv => pv.PictureId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ProductVariant>()
                .HasIndex(pv => pv.Sku)
                .IsUnique();

            modelBuilder.Entity<ProductVariant>()
                .Property(pv => pv.Price)
                .HasPrecision(18, 2);
        }

        private void ConfigureImage(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Image>()
                .HasOne(i => i.Variant)
                .WithMany(pv => pv.Images)
                .HasForeignKey(i => i.VariantId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        private void ConfigureAvailability(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Availability>()
                .HasOne(a => a.Variant)
                .WithMany(pv => pv.Availabilities)
                .HasForeignKey(a => a.VariantId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        private void ConfigureProductCategory(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProductCategory>()
                .HasKey(pc => new { pc.ProductId, pc.CategoryId });

            modelBuilder.Entity<ProductCategory>()
                .HasOne(pc => pc.Product)
                .WithMany(p => p.ProductCategories)
                .HasForeignKey(pc => pc.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductCategory>()
                .HasOne(pc => pc.Category)
                .WithMany(c => c.ProductCategories)
                .HasForeignKey(pc => pc.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        private void ConfigureProductTag(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProductTag>()
                .HasKey(pt => new { pt.ProductId, pt.TagId });

            modelBuilder.Entity<ProductTag>()
                .HasOne(pt => pt.Product)
                .WithMany(p => p.ProductTags)
                .HasForeignKey(pt => pt.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductTag>()
                .HasOne(pt => pt.Tag)
                .WithMany(t => t.ProductTags)
                .HasForeignKey(pt => pt.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        private void ConfigureIndexes(ModelBuilder modelBuilder)
        {
            // Product indexes
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Name);
            
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Audience);
            
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.MaterialId);
            
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.ParsingStatus);
            
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.ShopId);

            // ProductVariant indexes
            modelBuilder.Entity<ProductVariant>()
                .HasIndex(pv => pv.ProductId);
            
            modelBuilder.Entity<ProductVariant>()
                .HasIndex(pv => pv.Price);
            
            modelBuilder.Entity<ProductVariant>()
                .HasIndex(pv => new { pv.ProductId, pv.ColorId, pv.SizeId })
                .IsUnique();

            // Color indexes
            modelBuilder.Entity<Color>()
                .HasIndex(c => c.Name)
                .IsUnique();
            
            modelBuilder.Entity<Color>()
                .HasIndex(c => c.HexCode);

            // Size indexes
            modelBuilder.Entity<Size>()
                .HasIndex(s => s.Name)
                .IsUnique();

            // Material indexes
            modelBuilder.Entity<Material>()
                .HasIndex(m => m.Name)
                .IsUnique();

            // Tag indexes
            modelBuilder.Entity<Tag>()
                .HasIndex(t => t.Name)
                .IsUnique();

            // Image indexes
            modelBuilder.Entity<Image>()
                .HasIndex(i => i.VariantId);
            
            modelBuilder.Entity<Image>()
                .HasIndex(i => new { i.VariantId, i.SortOrder });

            // Availability indexes
            modelBuilder.Entity<Availability>()
                .HasIndex(a => a.VariantId);
            
            modelBuilder.Entity<Availability>()
                .HasIndex(a => a.IsAvailable);
        }
    }
} 