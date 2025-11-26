using chat_ya_backend.Models.Dtos.EntityDtos;
using chat_ya_backend.Models.Dtos.ErrorDtos;
using chat_ya_backend.Models.Dtos.RequestDtos;
using chat_ya_backend.Models.Dtos.ResponseDtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Serilog;
using System.Security.Claims;

namespace chat_ya_backend.Endpoints
{
    public static class UserEndpoints
    {
        /// <summary>
            /// La palabra clave this permite "extender" la clase WebApplication. 
            /// Esto significa que, en Program.cs, puedes simplemente escribir app.MapUserEndpoints(); como si fuera un método nativo de la aplicación
            /// Este método registrará(o mapeará) todas las rutas(los endpoints) que se definan dentro de él.
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static WebApplication MapUserEndpoints(this WebApplication app)
        {
            // Creamos un grupo de rutas base /api/users
            var group = app.MapGroup("/api/users")
                           .RequireAuthorization()//todos los endpoints de este grupo solo se pueden acceder si el cliente envia un token JWT válido
                           .WithOpenApi();

            // Reutilizamos el Logger Factory
            var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
            var userLogger = loggerFactory.CreateLogger("UserEndpoints");//sistema para obtener mensajes en la consola


            /// ClaimsPrincipal user. El objeto user contiene la identidad de la persona que hizo la solicitud
            /// UserManager<IdentityUser> userManager: herramienta principal de ASP.NET Identity para interactuar con la tabla de usuarios. 
            /// Lo usamos para buscar, crear o actualizar usuarios.
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