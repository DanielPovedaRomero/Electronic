using Microsoft.Data.SqlClient;
using System.Data;

namespace Electronic.Infrastructure.Data
{
    public sealed class SqlConnectionFactory(string connectionString) : IDbConnectionFactory
    {
        public async Task<IDbConnection> CreateConnection(CancellationToken cancellationToken)
        {
            var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
    }
}
