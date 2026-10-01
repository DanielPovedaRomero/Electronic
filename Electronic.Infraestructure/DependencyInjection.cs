using Electronic.Infrastructure.Constants;
using Electronic.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Electronic.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddDependencyInjectionInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString(Config.DBNameConnection)
                ?? throw new InvalidOperationException(
                       string.Format(Messages.NoConnectionMessage, Config.DBNameConnection));

            services.AddSingleton<IDbConnectionFactory>(new SqlConnectionFactory(connectionString));

            return services;
        }
    }
}
