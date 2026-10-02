using Electronic.Application.Common.Constants;
using Electronic.Application.Common.Responses;
using Electronic.Application.DTO;
using Electronic.Application.Mapping;
using Electronic.Application.Validators;
using Electronic.Domain.Repositories.Product;
using System.Net;

namespace Electronic.Application.Services.Product
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        public ProductService(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<ApiResponse<IReadOnlyList<ProductDto>>> GetAll(CancellationToken cancellationToken)
        {
            try
            {
                var products = await _productRepository.GetAll(cancellationToken);
                return ApiResponse<IReadOnlyList<ProductDto>>.Success(products.ToDtoList());
            }
            catch (Exception ex)
            {
                return ApiResponse<IReadOnlyList<ProductDto>>.Fail(HttpStatusCode.InternalServerError, Messages.InternalError);
            }
        }

        public async Task<ApiResponse<ProductDto>> GetBySku(string sku, CancellationToken cancellationToken)
        {
            try
            {
                var product = await _productRepository.GetBySku(sku, cancellationToken);

                if (product is null)
                    return ApiResponse<ProductDto>.Fail(HttpStatusCode.NotFound, string.Format(Messages.ProductNotFound, sku));

                return ApiResponse<ProductDto>.Success(product.ToDto());
            }
            catch (Exception ex)
            {
                return ApiResponse<ProductDto>.Fail(HttpStatusCode.InternalServerError, Messages.InternalError);
            }
        }

        public async Task<ApiResponse<ProductDto>> Create(ProductDto product, CancellationToken cancellationToken)
        {
            try
            {
                var validationError = ProductValidators.Validate(product);

                if (validationError is not null)
                    return ApiResponse<ProductDto>.Fail(HttpStatusCode.BadRequest, validationError);

                if (await _productRepository.Exists(product.Sku, cancellationToken))
                    ApiResponse<ProductDto>.Fail(HttpStatusCode.BadRequest, string.Format(Messages.ProductAlreadyExists, product.Sku));

                await _productRepository.Add(product.ToModel(), cancellationToken);
                return ApiResponse<ProductDto>.Success(product);
            }
            catch (Exception ex)
            {
                return ApiResponse<ProductDto>.Fail(HttpStatusCode.InternalServerError, Messages.InternalError);
            }
        }

        public async Task<ApiResponse<ProductDto>> Update(string sku, ProductDto product, CancellationToken cancellationToken)
        {
            try
            {
                if (!await _productRepository.Exists(product.Sku, cancellationToken))
                    ApiResponse<ProductDto>.Fail(HttpStatusCode.BadRequest, string.Format(Messages.ProductNotExists, product.Sku));

                var validationError = ProductValidators.Validate(product);

                if (validationError is not null)
                    return ApiResponse<ProductDto>.Fail(HttpStatusCode.BadRequest, validationError);

                await _productRepository.Update(product.ToModel(), cancellationToken);
                return ApiResponse<ProductDto>.Success(product);
            }
            catch (Exception ex)
            {
                return ApiResponse<ProductDto>.Fail(HttpStatusCode.InternalServerError, Messages.InternalError);
            }
        }

        public async Task<ApiResponse<bool>> Delete(string sku, CancellationToken cancellationToken)
        {
            try
            {
                if (!await _productRepository.Exists(sku, cancellationToken))
                    ApiResponse<ProductDto>.Fail(HttpStatusCode.BadRequest, string.Format(Messages.ProductNotExists, sku));

                await _productRepository.Delete(sku, cancellationToken);

                return ApiResponse<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Fail(HttpStatusCode.InternalServerError, Messages.InternalError);
            }
        }
    }
}
