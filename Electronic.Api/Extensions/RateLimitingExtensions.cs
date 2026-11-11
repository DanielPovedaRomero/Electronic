using Electronic.Application.Common.Responses;
using System.Net;
using System.Threading.RateLimiting;

namespace Electronic.Api.Extensions
{
    public static class RateLimitingExtensions
    {
        public const string PerIpPolicy = "PerIp";

        public static IServiceCollection AddDependencyInjectionRateLimiting(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.AddPolicy(PerIpPolicy, httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 100,                 
                            Window = TimeSpan.FromMinutes(1),  
                            QueueLimit = 0                   
                        }));

                options.OnRejected = async (context, cancellationToken) =>
                {
                    context.HttpContext.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                    await context.HttpContext.Response.WriteAsJsonAsync(
                        ApiResponse<string>.Fail(HttpStatusCode.TooManyRequests, "Too many requests. Try again later."),
                        cancellationToken);
                };
            });

            return services;
        }
    }
}
