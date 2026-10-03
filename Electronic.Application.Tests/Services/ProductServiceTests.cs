using Electronic.Application.DTO;
using Electronic.Application.Services.Product;
using Electronic.Domain.Entities.Product;
using Electronic.Domain.Repositories.Product;
using Moq;
using System.Net;

namespace Electronic.Application.Tests.Services
{
    public class ProductServiceTests
    {
        private readonly Mock<IProductRepository> _productRepository = new();
        private readonly ProductService _productService;

        public ProductServiceTests()
        {
            _productService = new ProductService(_productRepository.Object);
        }
        private static ProductDto ValidDto() => new() { Sku = "01", Name = "Product 1", Price = 500, Stock = 10 };

        public static TheoryData<List<ProductModel>> ProductLists => new()
        {
            new List<ProductModel>(),
            new List<ProductModel> { new() { Sku = "01", Name = "Product 1" } },
            new List<ProductModel>
            {
                new() { Sku = "02", Name = "Product 2" },
                new() { Sku = "03", Name = "Product 3" }
            }
        };

        [Theory]
        [MemberData(nameof(ProductLists))]
        public async Task GetAll_ReturnsOk(List<ProductModel> products)
        {
            _productRepository.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync(products);

            var response = await _productService.GetAll(CancellationToken.None);

            Assert.Equal((int)HttpStatusCode.OK, response.Code);
            Assert.Equal(products.Count, response.Result!.Count);
        }

        [Fact]
        public async Task GetAll_WhenRepositoryFails_ReturnsInternalServerError()
        {
            _productRepository.Setup(r => r.GetAll(It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("DB down"));

            var response = await _productService.GetAll(CancellationToken.None);

            Assert.Equal((int)HttpStatusCode.InternalServerError, response.Code);
        }

        [Fact]
        public async Task GetBySku_WhenProductExists_ReturnsOk()
        {
            _productRepository.Setup(r => r.GetBySku("01", It.IsAny<CancellationToken>())).ReturnsAsync(new ProductModel { Sku = "01", Name = "Product 1" });

            var response = await _productService.GetBySku("01", CancellationToken.None);

            Assert.Equal((int)HttpStatusCode.OK, response.Code);
            Assert.Equal("01", response.Result!.Sku);
        }

        [Fact]
        public async Task GetBySku_WhenProductDoesNotExist_ReturnsNotFound()
        {
            _productRepository.Setup(r => r.GetBySku("01", It.IsAny<CancellationToken>()))
                .ReturnsAsync((ProductModel?)null);

            var response = await _productService.GetBySku("01", CancellationToken.None);

            Assert.Equal((int)HttpStatusCode.NotFound, response.Code);
        }

        [Fact]
        public async Task Create_WhenValid_ReturnsOk()
        {
            _productRepository.Setup(r => r.Exists("01", It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var response = await _productService.Create(ValidDto(), CancellationToken.None);

            Assert.Equal((int)HttpStatusCode.OK, response.Code);
            _productRepository.Verify(r => r.Add(It.IsAny<ProductModel>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Create_WhenSkuAlreadyExists_ReturnsBadRequest()
        {
            _productRepository.Setup(r => r.Exists("01", It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var response = await _productService.Create(ValidDto(), CancellationToken.None);

            Assert.Equal((int)HttpStatusCode.BadRequest, response.Code);
            _productRepository.Verify(r => r.Add(It.IsAny<ProductModel>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Create_WhenNameIsEmpty_ReturnsBadRequest()
        {
            var dto = ValidDto();
            dto.Name = "";

            var response = await _productService.Create(dto, CancellationToken.None);

            Assert.Equal((int)HttpStatusCode.BadRequest, response.Code);
        }

        [Fact]
        public async Task Update_WhenProductExists_ReturnsOk()
        {
            _productRepository.Setup(r => r.Exists("01", It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var response = await _productService.Update(ValidDto(), CancellationToken.None);

            Assert.Equal((int)HttpStatusCode.OK, response.Code);
            _productRepository.Verify(r => r.Update(It.IsAny<ProductModel>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Update_WhenProductDoesNotExist_ReturnsBadRequest()
        {
            _productRepository.Setup(r => r.Exists("01", It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var response = await _productService.Update(ValidDto(), CancellationToken.None);

            Assert.Equal((int)HttpStatusCode.BadRequest, response.Code);
        }

        [Fact]
        public async Task Delete_WhenProductExists_ReturnsOk()
        {
            _productRepository.Setup(r => r.Exists("01", It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var response = await _productService.Delete("01", CancellationToken.None);

            Assert.Equal((int)HttpStatusCode.OK, response.Code);
            Assert.True(response.Result);
        }

        [Fact]
        public async Task Delete_WhenProductDoesNotExist_ReturnsBadRequest()
        {
            _productRepository.Setup(r => r.Exists("01", It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var response = await _productService.Delete("01", CancellationToken.None);

            Assert.Equal((int)HttpStatusCode.BadRequest, response.Code);
            _productRepository.Verify(r => r.Delete(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}