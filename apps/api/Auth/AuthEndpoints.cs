using FlagForge.Api.Contracts;
using FlagForge.Api.Data;
using FlagForge.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FlagForge.Api.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", async (LoginRequest req, AppDbContext db, TokenService tokens) =>
        {
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrEmpty(req.Password))
                return Results.Unauthorized();

            var email = req.Email.Trim().ToLowerInvariant();
            var user = await db.AdminUsers.SingleOrDefaultAsync(u => u.Email == email);

            if (user is null)
                return Results.Unauthorized();

            var result = new PasswordHasher<AdminUser>().VerifyHashedPassword(user, user.PasswordHash, req.Password);
            if (result == PasswordVerificationResult.Failed)
                return Results.Unauthorized();

            return Results.Ok(new LoginResponse(tokens.Create(user), user.Email));
        }).AllowAnonymous();

        app.MapGet("/auth/me", (ICurrentUser user) => Results.Ok(new { id = user.Id, email = user.Email }))
            .RequireAuthorization();

        return app;
    }
}
