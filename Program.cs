using Microsoft.Data.SqlClient;
using System.Data;
using Donora.Models.Repositories; // Ensure this namespace matches your files

var builder = WebApplication.CreateBuilder(args);

// 1. SERVICES CONFIGURATION
builder.Services.AddControllersWithViews();

// Retrieve and validate the connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// --- REPOSITORY REGISTRATION (Dependency Injection) ---
// We use AddScoped so a single instance is shared within a single web request.
builder.Services.AddScoped<InitiativeRepository>(sp => new InitiativeRepository(connectionString));
builder.Services.AddScoped<ContributionRepository>(sp => new ContributionRepository(connectionString));
builder.Services.AddScoped<UserRepository>(sp => new UserRepository(connectionString));

// REQUIRED: Enable Session services
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// 2. MIDDLEWARE PIPELINE
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// REQUIRED: Session middleware must be here
app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}")
    .WithStaticAssets();

app.Run();