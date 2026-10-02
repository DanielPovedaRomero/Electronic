namespace Electronic.Application.Common.Constants
{
    public static class Messages
    {
        public const string InternalError = "Internal error, please contact the administrator";
        public const string Succes = "Process completed successfully.";
        public const string ProductNotFound = "Product '{0}' was not found.";
        public const string ProductAlreadyExists = "A product with SKU '{0}' already exists.";
        public const string ProductNotExists = "A product with SKU '{0}' not exists.";
        public const string PriceNegative = "Price cannot be negative.";
        public const string StockNegative = "Stock cannot be negative.";
        public const string QuantityZero = "Quantity cannot be zero.";
        public const string SkuRequired = "Sku is required.";
        public const string NameRequired = "Name is required.";
    }
}
