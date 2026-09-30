using Electronic.Domain.Entities.Product;

namespace Electronic.Domain.Repositories.Product
{
    public interface IProductRepository
    {
        Task<IReadOnlyList<ProductModel>> GetAll(CancellationToken cancellationToken);
        Task<ProductModel?> GetBySku(string sku, CancellationToken cancellationToken);
        Task<bool> Exists(string sku, CancellationToken cancellationToken);
        Task Add(ProductModel product, CancellationToken cancellationToken);
        Task Update(ProductModel product, CancellationToken cancellationToken);
        Task Delete(string sku, CancellationToken cancellationToken);
    }
}
