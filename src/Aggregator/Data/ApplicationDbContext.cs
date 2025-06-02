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

        public DbSet<Product> Products { get; set; }

    }
} 