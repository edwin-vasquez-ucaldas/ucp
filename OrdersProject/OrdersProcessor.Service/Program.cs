
using CorrelationId;
using CorrelationId.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OrdersProcessor.Service.Data;
using OrdersProcessor.Service.Services;
using Prometheus;
using Serilog;
using Serilog.Enrichers.CorrelationId;
using Serilog.Enrichers.Span;

namespace OrdersProcessor.Service
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Configure Serilog early
            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .AddJsonFile($"appsettings.{environmentName}.json", true)
                    .AddEnvironmentVariables()
                    .Build())
                .Enrich.WithCorrelationId()
                .Enrich.WithSpan()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(
                    path: "logs/orders-processor-.log",
                    formatter: new Serilog.Formatting.Json.JsonFormatter(renderMessage: true),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    shared: true)
                .CreateLogger();

            try
            {
                Log.Information("Starting Orders Processor Service application");

                var builder = WebApplication.CreateBuilder(args);

                // Replace default logging with Serilog
                builder.Host.UseSerilog();

                // Add Correlation ID services
                builder.Services.AddDefaultCorrelationId(options =>
                {
                    options.CorrelationIdGenerator = () => Guid.NewGuid().ToString("N")[..12];
                    options.AddToLoggingScope = true;
                    options.LoggingScopeKey = "CorrelationId";
                    options.IncludeInResponse = true;
                });

                // Add services to the container.
                builder.Services.AddControllers();

                // Add Entity Framework
                builder.Services.AddDbContext<OrderDbContext>(options =>
                    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

                // Add Health Checks
                builder.Services.AddHealthChecks();

                // Configure Order Processor settings
                builder.Services.Configure<OrderProcessorConfiguration>(
                    builder.Configuration.GetSection("OrderProcessor"));

                // Add the background service
                builder.Services.AddHostedService<OrderStatusProcessorService>();

                // Configure OpenTelemetry
                var serviceName = builder.Configuration["OpenTelemetry:ServiceName"] ?? "orders-processor";
                var serviceVersion = builder.Configuration["OpenTelemetry:ServiceVersion"] ?? "1.0.0";
                var otlpEndpoint = builder.Configuration["OpenTelemetry:Exporters:OTLP:Endpoint"] ?? "http://localhost:4317";

                builder.Services.AddOpenTelemetry()
                    .ConfigureResource(resource => resource
                        .AddService(serviceName, serviceVersion)
                        .AddAttributes(new Dictionary<string, object>
                        {
                            ["deployment.environment"] = builder.Environment.EnvironmentName,
                            ["service.instance.id"] = Environment.MachineName,
                        }))
                    .WithTracing(tracing => tracing
                        .AddAspNetCoreInstrumentation(options =>
                        {
                            options.RecordException = true;
                            options.EnrichWithHttpRequest = (activity, request) =>
                            {
                                activity.SetTag("http.request.header.correlation_id",
                                    request.Headers["X-Correlation-ID"].FirstOrDefault());
                            };
                        })
                        .AddEntityFrameworkCoreInstrumentation(options =>
                        {
                            options.SetDbStatementForText = true;
                            options.SetDbStatementForStoredProcedure = true;
                        })
                        .AddHttpClientInstrumentation()
                        .AddSqlClientInstrumentation(options =>
                        {
                            options.SetDbStatementForText = true;
                            options.RecordException = true;
                        })
                        .AddOtlpExporter(options =>
                        {
                            options.Endpoint = new Uri(otlpEndpoint);
                        }))
                    .WithMetrics(metrics => metrics
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddOtlpExporter(options =>
                        {
                            options.Endpoint = new Uri(otlpEndpoint);
                        }));

                // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                    {
                        Title = "Orders Processor Service",
                        Version = "v1",
                        Description = "Background service for processing order status updates with observability"
                    });
                });

                // Add CORS support
                builder.Services.AddCors(options =>
                {
                    options.AddPolicy("AllowAll", policy =>
                    {
                        policy.AllowAnyOrigin()
                              .AllowAnyMethod()
                              .AllowAnyHeader();
                    });
                });

                var app = builder.Build();

                // Use Correlation ID middleware (must be early in pipeline)
                app.UseCorrelationId();

                // Configure the HTTP request pipeline.
                if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI(c =>
                    {
                        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Orders Processor Service v1");
                    });
                }

                // Add Serilog request logging
                app.UseSerilogRequestLogging(options =>
                {
                    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                    {
                        var correlationId = httpContext.TraceIdentifier;
                        diagnosticContext.Set("CorrelationId", correlationId);
                        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                        diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].FirstOrDefault());
                    };
                });

                app.UseCors("AllowAll");
                app.UseAuthorization();

                // Add Health Check endpoint
                app.MapHealthChecks("/health");

                app.UseHttpMetrics();
                app.MapMetrics("/metrics");

                app.MapControllers();

                // Ensure database exists
                using (var scope = app.Services.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

                    try
                    {
                        context.Database.EnsureCreated();
                        logger.LogInformation("Database connection verified");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error connecting to database");
                    }
                }

                Log.Information("Orders Processor Service configured successfully, starting application");
                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Orders Processor Service application failed to start");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
