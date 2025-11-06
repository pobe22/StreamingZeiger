using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StreamingZeiger;
using StreamingZeiger.Data;
using StreamingZeiger.Models;
using StreamingZeiger.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Environment Variablen laden ---
DotNetEnv.Env.Load();

// --- Services registrieren ---
builder.Services.AddScoped<ITmdbService, TmdbService>();
builder.Services.AddScoped<LoggingService>();
builder.Services.AddScoped<DatabaseBackupService>();

// --- Datenbank konfigurieren ---
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var env = builder.Environment;
    var configuration = builder.Configuration;

    // Prüfen, ob wir auf Render oder in Production laufen
    var usePostgres = Environment.GetEnvironmentVariable("RENDER") == "true" ||
                      env.IsProduction();

    if (usePostgres)
    {
        // PostgreSQL-Verbindung (Render)
        var connectionString = configuration.GetConnectionString("PostgresConnection");
        options.UseNpgsql(connectionString);
    }
    else
    {
        // Lokale SQLite-Verbindung
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        options.UseSqlite(connectionString);
    }

    options.EnableSensitiveDataLogging();
});

// --- Identity konfigurieren ---
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
});

// --- Weitere Services ---
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<IStaticMovieRepository, StaticMovieRepository>();
builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<DynamicDbContextFactory>();

var app = builder.Build();

// --- Middleware ---
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

// --- Routen ---
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// --- Datenbank initialisieren ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    await context.Database.MigrateAsync();

    // Nur für SQLite aktivieren
    var env = app.Environment;
    var usePostgres = Environment.GetEnvironmentVariable("RENDER") == "true" ||
                      env.IsProduction();

    if (!usePostgres)
    {
        var conn = context.Database.GetDbConnection();
        await conn.OpenAsync();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "PRAGMA foreign_keys = ON;";
            await cmd.ExecuteNonQueryAsync();
        }
    }

    await DbInitializer.InitializeAsync(context, userManager, roleManager);
}

app.Run();
