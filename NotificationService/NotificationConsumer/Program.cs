
using NotificationConsumer.Services;

namespace NotificationConsumer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Register notification services
            builder.Services.AddSingleton<EmailNotificationService>();

            // Register services for DI
            builder.Services.AddScoped<IEmailNotificationService>(provider =>
                provider.GetRequiredService<EmailNotificationService>());

            // Register background services
            builder.Services.AddHostedService<EmailNotificationBackgroundService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
