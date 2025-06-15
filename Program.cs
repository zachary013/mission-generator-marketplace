using SmartMarketplace.Configuration;
using SmartMarketplace.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure specific ports
builder.WebHost.UseUrls("http://localhost:5000", "https://localhost:5001");

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "SmartMarketplace API", 
        Version = "v1",
        Description = "API pour la génération intelligente de missions freelance avec IA"
    });
    
    // Include XML comments for better documentation
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Configure AI settings
builder.Services.Configure<AIConfig>(builder.Configuration.GetSection("AI"));

// Register HTTP clients for AI services with timeout configuration
builder.Services.AddHttpClient<IGeminiService, GeminiService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddHttpClient<IDeepSeekService, DeepSeekService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddHttpClient<IMistralService, MistralService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Register services
builder.Services.AddScoped<IGeminiService, GeminiService>();
builder.Services.AddScoped<IDeepSeekService, DeepSeekService>();
builder.Services.AddScoped<IMistralService, MistralService>();
builder.Services.AddScoped<IAIService, AIService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SmartMarketplace API v1");
        c.RoutePrefix = string.Empty; // Swagger UI at root
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Add health check endpoint
app.MapGet("/health", () => new { 
    Status = "Healthy", 
    Timestamp = DateTime.UtcNow,
    Version = "1.0.0"
});

// Log startup information
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("🚀 SmartMarketplace API started successfully");
logger.LogInformation("📖 Swagger UI available at: http://localhost:5000 and https://localhost:5001");

app.Run();
