using chat_ya_backend.Models.Dtos.EntityDtos;
using chat_ya_backend.Models.Dtos.ErrorDtos;
using chat_ya_backend.Models.Dtos.RequestDtos;
using chat_ya_backend.Models.Dtos.ResponseDtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace chat_ya_backend.Endpoints
{
    public static class UserEndpoints
    {
        public static WebApplication MapUserEndpoints(this WebApplication app)
        {
            // Creamos un grupo de rutas base /api/users, todas requieren autorización
            var group = app.MapGroup("/api/users")
                           .RequireAuthorization()
                           .WithOpenApi();

            // Reutilizamos el Logger Factory
            var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
            var userLogger = loggerFactory.CreateLogger("UserEndpoints");

            // --- 1. GET /api/users/me (Obtener Perfil Propio) ---
            group.MapGet("/me", async (ClaimsPrincipal user, UserManager<IdentityUser> userManager) =>
            {
                // 1. Obtener el ID del token JWT
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Results.Unauthorized();
                }

                // 2. Buscar el usuario en Identity
                var identityUser = await userManager.FindByIdAsync(userId);
                if (identityUser == null)
                {
                    // Esto no debería pasar si el token es válido
                    userLogger.LogError("Token válido, pero usuario con ID {UserId} no encontrado.", userId);
                    return Results.NotFound(new GenericErrorDto("Usuario no encontrado.", "User.Me"));
                }

                // 3. Mapear a DTO de respuesta
                var userDto = new UserDto
                {
                    Id = identityUser.Id,
                    Username = identityUser.UserName ?? string.Empty
                };

                return Results.Ok(userDto);
            })
            .WithName("GetUserProfile");

            // --- 2. GET /api/users/{id} (Obtener Perfil por ID) ---
            group.MapGet("/{id}", async (string id, UserManager<IdentityUser> userManager) =>
            {
                // 1. Buscar el usuario en Identity
                var identityUser = await userManager.FindByIdAsync(id);
                if (identityUser == null)
                {
                    userLogger.LogWarning("Intento de obtener perfil de ID no encontrado: {Id}", id);
                    return Results.NotFound(new GenericErrorDto("Usuario no encontrado.", "User.ById"));
                }

                // 2. Mapear a DTO de respuesta 
                var userDto = new UserDto
                {
                    Id = identityUser.Id,
                    Username = identityUser.UserName ?? string.Empty
                };

                return Results.Ok(userDto);
            })
            .WithName("GetUserById");


            return app;
        }
    }
}