using Electronic.Application.Services.Product;
using Microsoft.Extensions.DependencyInjection;

namespace Electronic.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IProductService, ProductService>();
            return services;
        }
    }
}
