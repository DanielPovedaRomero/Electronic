using Electronic.Application.Common.Responses;
using Electronic.Application.DTO;
using System.Net;

namespace Electronic.Application.Services.Product
{
    public class ProductService
    {
        public ProductService()
        {
                
        }
        public async Task<ApiResponse<IReadOnlyList<ProductDto>>> GetAll(CancellationToken cancellationToken) 
        {
            try
            {
                var products = await productRepository.GetAll(cancellationToken);
                return ApiResponse<IReadOnlyList<ProductDto>>.Success(products.Select(ToDto).ToList());
            }
            catch (Exception ex)
            {
                return ApiResponse<IReadOnlyList<ProductDto>>.Fail(HttpStatusCode.InternalServerError, "Error interno comuniquese con el administrador");
            }
        }
        private static ProductDto ToDto(ProductModel product) => new()
        {
            Sku = product.Sku,
            Name = product.Name,
            Price = product.Price,
            Stock = product.Stock
        };
    }
}
