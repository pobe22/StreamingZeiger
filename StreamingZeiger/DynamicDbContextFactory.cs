using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using StreamingZeiger.Data;
using StreamingZeiger.Models;
using System.Security.Claims;

namespace StreamingZeiger
{
    public class DynamicDbContextFactory
    {
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;

        public DynamicDbContextFactory(IConfiguration config, IHttpContextAccessor httpContextAccessor, UserManager<ApplicationUser> userManager)
        {
            _config = config;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
        }

        public async Task<AppDbContext> CreateDbContextAsync()
        {
            var user = _httpContextAccessor.HttpContext?.User;

            string connectionString;

            if (user?.Identity?.IsAuthenticated == true)
            {
                var appUser = await _userManager.GetUserAsync(user);
                var roles = await _userManager.GetRolesAsync(appUser);

                if (roles.Contains("Redakteur"))
                    connectionString = _config.GetConnectionString("EditorConnection");
                else
                    connectionString = _config.GetConnectionString("ViewerConnection");
            }
            else
            {
                // Nicht eingeloggt -> nur lesen
                connectionString = _config.GetConnectionString("ViewerConnection");
            }

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connectionString)
                .EnableSensitiveDataLogging()
                .Options;

            return new AppDbContext(options);
        }
    }
}
