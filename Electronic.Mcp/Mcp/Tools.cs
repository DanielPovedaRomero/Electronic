using Electronic.Application.Common.Responses;
using Electronic.Application.DTO;
using Electronic.Application.Services.Product;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Electronic.Mcp.Mcp
{
    [McpServerToolType]
    public class ProductTools
    {
        private readonly IProductService _productService;

        public ProductTools(IProductService productService)
        {
            _productService = productService;
        }

        [McpServerTool, Description("Gets all products in the inventory with SKU, name, price and stock.")]
        public async Task<ApiResponse<IReadOnlyList<ProductDto>>> GetProducts(CancellationToken cancellationToken)
        {
            return await _productService.GetAll(cancellationToken);
        }
    }
}
