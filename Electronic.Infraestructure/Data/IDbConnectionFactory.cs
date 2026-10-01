using System.Data;

namespace Electronic.Infrastructure.Data
{
    public interface IDbConnectionFactory
    {
        Task<IDbConnection> CreateConnection(CancellationToken cancellationToken);
    }
}
