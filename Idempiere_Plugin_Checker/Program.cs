using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Idempiere_Plugin_Checker.DB;
using Idempiere_Plugin_Checker.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configurar Autenticación con Cookies para el módulo de Administración
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Admin/Login";
        options.LogoutPath = "/Admin/Logout";
        options.AccessDeniedPath = "/Admin/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// Configurar Entity Framework Core con SQLite
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=DbMain.db";
builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlite(connectionString));

// Registrar servicios
builder.Services.AddScoped<JsonExtractor>();

// Registrar Worker en segundo plano para consultas periódicas
builder.Services.AddHostedService<PluginSyncWorker>();

var app = builder.Build();

// Asegurar creación de base de datos SQLite y seed inicial
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DataContext>();
    db.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

