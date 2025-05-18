using System.Collections.Generic;
using System.Threading.Tasks;
using Aggregator.Models;

namespace Aggregator.Interfaces
{
    public interface IParser
    {
        string ShopName { get; }
        Task<List<Product>> ParseProducts();
        Task ParseAsync();
    }
}