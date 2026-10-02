using Electronic.Application.Common.Responses;
using Electronic.Application.DTO;

namespace Electronic.Application.Services.Product
{
    public interface IProductService
    {
        Task<ApiResponse<IReadOnlyList<ProductDto>>> GetAll(CancellationToken cancellationToken);
        Task<ApiResponse<ProductDto>> GetBySku(string sku, CancellationToken cancellationToken);
        Task<ApiResponse<ProductDto>> Create(ProductDto product, CancellationToken cancellationToken);
        Task<ApiResponse<ProductDto>> Update(string sku, ProductDto product, CancellationToken cancellationToken);
        Task<ApiResponse<bool>> Delete(string sku, CancellationToken cancellationToken);
    }
}
