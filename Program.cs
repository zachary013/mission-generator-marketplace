var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    // Set CreateMission as the default page
    options.Conventions.AddPageRoute("/CreateMission", "");
});

// Register all services
builder.Services.AddHttpClient<SmartMarketplace.Services.IGrokService, SmartMarketplace.Services.GrokService>();
builder.Services.AddScoped<SmartMarketplace.Services.IInputAnalysisService, SmartMarketplace.Services.InputAnalysisService>();
builder.Services.AddScoped<SmartMarketplace.Services.IMissionTemplateService, SmartMarketplace.Services.MissionTemplateService>();
builder.Services.AddScoped<SmartMarketplace.Services.IPromptService, SmartMarketplace.Services.PromptService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // Redirect to CreateMission page in case of errors
    app.UseExceptionHandler("/CreateMission");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
    .WithStaticAssets();

app.Run();
