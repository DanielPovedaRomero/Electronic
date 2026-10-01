using Dapper;
using Electronic.Domain.Entities.Product;
using Electronic.Domain.Repositories.Product;
using Electronic.Infrastructure.Data;
using System.Data;

namespace Electronic.Infrastructure.Repositories.Product
{
    public class ProductRepository(IDbConnectionFactory connectionFactory) : IProductRepository
    {
        public async Task<IReadOnlyList<ProductModel>> GetAll(CancellationToken cancellationToken = default)
        {
            using var connection = await connectionFactory.CreateConnection(cancellationToken);

            var command = new CommandDefinition(
                commandText: "SP_Product_GetAll",
                parameters: null,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken);

            var products = await connection.QueryAsync<ProductModel>(command);
            return products.ToList();
        }

        public async Task<ProductModel?> GetBySku(string sku, CancellationToken cancellationToken = default)
        {
            using var connection = await connectionFactory.CreateConnection(cancellationToken);

            var parameters = new DynamicParameters();
            parameters.Add("@Sku", sku, DbType.String);

            var command = new CommandDefinition(
                commandText: "SP_Product_GetBySku",
                parameters: parameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken);

            return await connection.QuerySingleOrDefaultAsync<ProductModel>(command);
        }

        public async Task<bool> Exists(string sku, CancellationToken cancellationToken = default)
        {
            using var connection = await connectionFactory.CreateConnection(cancellationToken);

            var parameters = new DynamicParameters();
            parameters.Add("@Sku", sku, DbType.String);

            var command = new CommandDefinition(
                commandText: "SP_Product_Exists",
                parameters: parameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken);

            return await connection.ExecuteScalarAsync<bool>(command);
        }

        public async Task Add(ProductModel product, CancellationToken cancellationToken = default)
        {
            using var connection = await connectionFactory.CreateConnection(cancellationToken);

            var parameters = new DynamicParameters();
            parameters.Add("@Sku", product.Sku, DbType.String);
            parameters.Add("@Name", product.Name, DbType.String);
            parameters.Add("@Price", product.Price, DbType.Decimal);
            parameters.Add("@Stock", product.Stock, DbType.Int32);

            var command = new CommandDefinition(
                commandText: "SP_Product_Insert",
                parameters: parameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken);

            await connection.ExecuteAsync(command);
        }

        public async Task Update(ProductModel product, CancellationToken cancellationToken = default)
        {
            using var connection = await connectionFactory.CreateConnection(cancellationToken);

            var parameters = new DynamicParameters();
            parameters.Add("@Sku", product.Sku, DbType.String);
            parameters.Add("@Name", product.Name, DbType.String);
            parameters.Add("@Price", product.Price, DbType.Decimal);
            parameters.Add("@Stock", product.Stock, DbType.Int32);

            var command = new CommandDefinition(
                commandText: "SP_Product_Update",
                parameters: parameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken);

            await connection.ExecuteAsync(command);
        }

        public async Task Delete(string sku, CancellationToken cancellationToken = default)
        {
            using var connection = await connectionFactory.CreateConnection(cancellationToken);

            var parameters = new DynamicParameters();
            parameters.Add("@Sku", sku, DbType.String);

            var command = new CommandDefinition(
                commandText: "SP_Product_Delete",
                parameters: parameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken);

            await connection.ExecuteAsync(command);
        }
    }
}
