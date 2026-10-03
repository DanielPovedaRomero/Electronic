using Electronic.Application.DTO;
using Electronic.Application.Services.Product;
using Microsoft.AspNetCore.Mvc;

namespace Electronic.Api.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var response = await _productService.GetAll(cancellationToken);
            return StatusCode(response.Code, response);
        }

        [HttpGet("{sku}")]
        public async Task<IActionResult> GetBySku(string sku, CancellationToken cancellationToken)
        {
            var response = await _productService.GetBySku(sku, cancellationToken);
            return StatusCode(response.Code, response);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProductDto product, CancellationToken cancellationToken)
        {
            var response = await _productService.Create(product, cancellationToken);
            return StatusCode(response.Code, response);
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] ProductDto product, CancellationToken cancellationToken)
        {
            var response = await _productService.Update(product, cancellationToken);
            return StatusCode(response.Code, response);
        }

        [HttpDelete("{sku}")]
        public async Task<IActionResult> Delete(string sku, CancellationToken cancellationToken)
        {
            var response = await _productService.Delete(sku, cancellationToken);
            return StatusCode(response.Code, response);
        }
    }
}