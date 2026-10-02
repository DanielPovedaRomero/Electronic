using Electronic.Application.DTO;
using Electronic.Domain.Entities.Product;

namespace Electronic.Application.Mapping
{
    public static class ProductMapping
    {
        public static ProductDto ToDto(this ProductModel product) => new()
        {
            Sku = product.Sku,
            Name = product.Name,
            Price = product.Price,
            Stock = product.Stock
        };

        public static ProductModel ToModel(this ProductDto product) => new()
        {
            Sku = product.Sku,
            Name = product.Name,
            Price = product.Price,
            Stock = product.Stock
        };

        public static IReadOnlyList<ProductDto> ToDtoList(this IEnumerable<ProductModel> products) =>
            products.Select(product => product.ToDto()).ToList();
    }
}
