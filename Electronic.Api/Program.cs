using Electronic.Api.Extensions;
using Electronic.Application;
using Electronic.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddDependencyInjectionRateLimiting(); 

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();                 
    app.MapScalarApiReference();      
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.MapControllers().RequireRateLimiting(RateLimitingExtensions.PerIpPolicy); ;

app.Run();