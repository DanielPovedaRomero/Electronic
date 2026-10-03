using Electronic.Domain.Repositories.Product;
using Electronic.Infrastructure.Constants;
using Electronic.Infrastructure.Data;
using Electronic.Infrastructure.Repositories.Product;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Electronic.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString(Config.DBNameConnection)
                ?? throw new InvalidOperationException(
                       string.Format(Messages.NoConnectionMessage, Config.DBNameConnection));

            services.AddSingleton<IDbConnectionFactory>(new SqlConnectionFactory(connectionString));
            services.AddScoped<IProductRepository, ProductRepository>();

            return services;
        }
    }
}
