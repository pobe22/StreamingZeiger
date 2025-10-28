using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using StreamingZeiger;
using StreamingZeiger.Data;
using StreamingZeiger.Models;
using StreamingZeiger.Services;

var builder = WebApplication.CreateBuilder(args);

DotNetEnv.Env.Load();
builder.Services.AddScoped<ITmdbService, TmdbService>();
builder.Services.AddScoped<LoggingService>();
builder.Services.AddScoped<DatabaseBackupService>();


builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))
           .EnableSensitiveDataLogging()
);

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
});

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<IStaticMovieRepository, StaticMovieRepository>();

builder.Services.AddSession();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<DynamicDbContextFactory>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession(); // Session kommt hier rein

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    await DbInitializer.InitializeAsync(context, userManager, roleManager);
}

app.Run();
