using IzweAutoService.Api.BackgroundServices;
using IzweAutoService.Infrastructure;
using IzweAutoService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString("Default") ?? "Data Source=izwe_sms.db",
    builder.Configuration.GetValue<string>("DatabaseProvider") ?? "sqlite"
);
builder.Services.AddHostedService<SmsProcessingJob>();

var app = builder.Build();

// Auto-migrate database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

// Serve React SPA from wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

// Fallback to index.html for SPA routing
app.MapFallbackToFile("index.html");

app.Run();
