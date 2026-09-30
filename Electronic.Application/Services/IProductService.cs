using Electronic.Application.DTO;

namespace Electronic.Application.Services
{
    public interface IProductService
    {
        Task<IReadOnlyList<ProductDto>> GetAll(CancellationToken cancellationToken);
        Task<ProductDto> GetBySku(string sku, CancellationToken cancellationToken);
        Task<ProductDto> Create(ProductDto product, CancellationToken cancellationToken);
        Task<ProductDto> Update(string sku, ProductDto product, CancellationToken cancellationToken);
        Task<ProductDto> AdjustStock(string sku, int quantity, CancellationToken cancellationToken);
        Task Delete(string sku, CancellationToken cancellationToken);
    }
}
