using Electronic.Application.Common.Constants;
using Electronic.Application.DTO;

namespace Electronic.Application.Validators
{
    public static class ProductValidators
    {
        public static string? Validate(ProductDto product)
        {
            if (string.IsNullOrWhiteSpace(product.Sku))
                return Messages.SkuRequired;

            if (string.IsNullOrWhiteSpace(product.Name))
                return Messages.NameRequired;

            if (product.Stock < 0)
                return Messages.StockNegative;

            if (product.Price < 0)
                return Messages.PriceNegative;

            return null;
        }
    }
}
