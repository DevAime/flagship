using FlagForge.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FlagForge.Api.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();

        var email = app.Configuration["Admin:Email"];
        var password = app.Configuration["Admin:Password"];

        if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password) && !await db.AdminUsers.AnyAsync())
        {
            var user = new AdminUser
            {
                Id = Guid.NewGuid(),
                Email = email.Trim().ToLowerInvariant(),
                CreatedAt = DateTimeOffset.UtcNow
            };
            user.PasswordHash = new PasswordHasher<AdminUser>().HashPassword(user, password);
            db.AdminUsers.Add(user);
            await db.SaveChangesAsync();
        }
    }
}
