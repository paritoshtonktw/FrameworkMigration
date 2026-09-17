using Serilog;
using CryptoTrading.Core.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure Serilog for Structured Logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// 2. Register Web API Controllers with camelCase serialization
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// 3. Register Open API / Swagger Support
builder.Services.AddOpenApi();

// 4. Register Core Application Layers using decoupled Extension Methods
builder.Services
    .AddCoreDatabase(builder.Configuration)
    .AddCoreRepositories()
    .AddCoreServices()
    .AddCoreAuthentication(builder.Configuration);

// 5. Configure Modern CORS Matching the React Web App Origin
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173") // React Dev Ports
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// 6. Global Error-Handling Middleware (Envelope-compliant API Errors)
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Unhandled exception occurred while processing request {Path}", context.Request.Path);
        
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var errorEnvelope = new
        {
            success = false,
            message = "An unexpected error occurred on the server.",
            errorCode = "INTERNAL_SERVER_ERROR"
        };

        await context.Response.WriteAsJsonAsync(errorEnvelope);
    }
});

// 7. Pipeline Routing Setup
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// 8. Swagger / OpenAPI Endpoint Routing
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "CryptoTrading Core API v1");
        options.RoutePrefix = "swagger"; // Enables Swagger UI at /swagger
    });
}

// 9. Map Web API Controllers
app.MapControllers();

app.Run();
