
using API.Quotes.Limit.Services;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace API.Quotes.Limit
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();

            // Register Redis connection multiplexer
            builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(sp =>
                StackExchange.Redis.ConnectionMultiplexer.Connect("localhost:6379")
            );

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddSingleton<IRateLimiterService, RateLimiterService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthorization();


            app.MapControllers();

            app.Use(async (context, next) =>
            {
                var rateLimiterService = context.RequestServices.GetRequiredService<IRateLimiterService>();
                var acquired = await rateLimiterService.AcquireLimit(rateLimiterService._globalLimiter);
                if (!acquired)
                {
                    context.Response.StatusCode = 429; // Too Many Requests
                    await context.Response.WriteAsync("Global rate limit exceeded. Try again later.");
                    return;
                }
                await next();
            });

            app.Run();
        }
    }
}
