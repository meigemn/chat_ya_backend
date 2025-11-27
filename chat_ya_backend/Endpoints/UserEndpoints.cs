using chat_ya_backend.Models.Dtos.CreateDtos; 
using chat_ya_backend.Models.Dtos.EntityDtos;
using chat_ya_backend.Models.Dtos.ErrorDtos;
using chat_ya_backend.Models.Dtos.RequestDtos;
using chat_ya_backend.Models.Dtos.ResponseDtos;
using chat_ya_backend.Models.Dtos.UpdateDto;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Swashbuckle.AspNetCore.Annotations;



namespace chat_ya_backend.Endpoints
{
    public static class UserEndpoints
    {
        public static WebApplication MapUserEndpoints(this WebApplication app)

        {
            var response = new CreateEditRemoveResponseDto();
            response.IsSuccess(0);// de UpdateUserDto

            // Creamos un grupo de rutas base /api/users, todas requieren autorización
            var group = app.MapGroup("/api/users")
                           .WithTags("Users")
                           .WithOpenApi();

            // Reutilizamos el Logger Factory
            var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
            var userLogger = loggerFactory.CreateLogger("UserEndpoints");

            // --- 3. GET /api/users (Listar Todos los Usuarios) ---
            group.MapGet("/", async (UserManager<IdentityUser> userManager) =>
            {
                userLogger.LogInformation("Solicitud para listar todos los usuarios.");

                // 1. Obtener todos los usuarios, proyectando directamente al DTO
                var allUsersDto = await userManager.Users
                                                .Select(user => new UserDto
                                                {
                                                    Id = user.Id,
                                                    UserName = user.UserName ?? string.Empty,
                                                    Email = user.Email ?? string.Empty,
                                                })
                                                .ToListAsync();

                // 2. Devolver la lista (arreglo JSON) directamente
                return Results.Ok(allUsersDto); // Devuelve List<UserDto> y código 200 OK
            })
            .WithName("GetAllUsers");
            // --- 1. GET /api/users/{id} (Obtener Perfil por ID) ---
            group.MapGet("/{id}", async (string id, UserManager<IdentityUser> userManager) =>
            {
                // 1. Buscar el usuario en Identity
                var identityUser = await userManager.FindByIdAsync(id);
                if (identityUser == null)
                {
                    userLogger.LogWarning("Intento de obtener perfil de ID no encontrado: {Id}", id);
                    return Results.NotFound(new GenericErrorDto("Usuario no encontrado.", "User.ById"));
                }

                // 2. Mapear a DTO de respuesta (No incluimos el Email por defecto a terceros)
                var userDto = new UserDto
                {
                    Id = identityUser.Id,
                    UserName = identityUser.UserName ?? string.Empty,
                    Email = identityUser.Email ?? string.Empty,
                };

                return Results.Ok(userDto);
            })
            .WithName("GetUserById");



            // --- 1. GET /api/users/me (Obtener Perfil Propio) ---
            group.MapGet("/me", async (ClaimsPrincipal user, UserManager<IdentityUser> userManager) =>
            {
                // 1. Obtener el ID del token JWT
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    // Si el token pasó la autorización pero no tiene el claim ID (lo cual es raro), es no autorizado.
                    return Results.Unauthorized();
                }

                // 2. Buscar el usuario en Identity
                var identityUser = await userManager.FindByIdAsync(userId);
                if (identityUser == null)
                {
                    userLogger.LogError("Token válido, pero usuario con ID {UserId} no encontrado.", userId);
                    return Results.NotFound(new GenericErrorDto("Usuario no encontrado.", "User.Me"));
                }

                // 3. Mapear a DTO de respuesta
                var userDto = new UserDto
                {
                    Id = identityUser.Id,
                    UserName = identityUser.UserName ?? string.Empty,
                    Email = identityUser.Email ?? string.Empty
                    
                };

                return Results.Ok(userDto);
            })
            .WithName("GetUserProfile");

            
            // --- 3. PUT /api/users/me (Actualizar Perfil Propio) ---
            group.MapPut("/me", async (
                UpdateUserDto model,
                ClaimsPrincipal user,
                UserManager<IdentityUser> userManager) =>
            {
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                // 1. Verificar ID
                if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

                // 2. Buscar usuario
                var identityUser = await userManager.FindByIdAsync(userId);
                if (identityUser == null)
                {
                    return Results.NotFound(new GenericErrorDto("Usuario no encontrado.", "User.Update"));
                }

                bool changed = false;

                // 3. Actualizar Username (si se proporciona y es diferente)
                if (!string.IsNullOrEmpty(model.NewUserName) && model.NewUserName != identityUser.UserName)
                {
                    var setUserNameResult = await userManager.SetUserNameAsync(identityUser, model.NewUserName);
                    if (!setUserNameResult.Succeeded)
                    {
                        userLogger.LogError("Fallo al actualizar nombre para {UserId}: {Errors}", userId, string.Join(", ", setUserNameResult.Errors.Select(e => e.Description)));
                        return Results.BadRequest(new CreateEditRemoveResponseDto { Success = false, Errors = setUserNameResult.Errors.Select(e => e.Description).ToList() });
                    }
                    changed = true;
                }

                // 4. Actualizar Email (si se proporciona y es diferente)
                if (!string.IsNullOrEmpty(model.NewEmail) && model.NewEmail != identityUser.Email)
                {
                    var setEmailResult = await userManager.SetEmailAsync(identityUser, model.NewEmail);
                    if (!setEmailResult.Succeeded)
                    {
                        userLogger.LogError("Fallo al actualizar email para {UserId}: {Errors}", userId, string.Join(", ", setEmailResult.Errors.Select(e => e.Description)));
                        return Results.BadRequest(new CreateEditRemoveResponseDto { Success = false, Errors = setEmailResult.Errors.Select(e => e.Description).ToList() });
                    }
                    changed = true;
                }

                if (changed)
                {
                    // 5. Actualizar el sello de seguridad. Esto invalida los tokens antiguos forzando un nuevo login
                    // si el token no tiene el security stamp (una buena práctica de seguridad).
                    await userManager.UpdateSecurityStampAsync(identityUser);
                    userLogger.LogInformation("Perfil de usuario {UserId} actualizado con éxito.", userId);
                }

               
                return Results.Ok(response);
            })
            .WithName("UpdateUserProfile");
            // --- 4. POST /api/users/change-password (Cambiar Contraseña) ---
            group.MapPost("/change-password", async (
                ChangePasswordDto model,
                ClaimsPrincipal user,
                UserManager<IdentityUser> userManager) =>
            {
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

                var identityUser = await userManager.FindByIdAsync(userId);
                if (identityUser == null)
                {
                    return Results.NotFound(new GenericErrorDto("Usuario no encontrado.", "User.ChangePassword"));
                }

                // 1. Intentar cambiar la contraseña. Identity valida la contraseña actual.
                var result = await userManager.ChangePasswordAsync(identityUser, model.CurrentPassword, model.NewPassword);

                if (result.Succeeded)
                {
                    // 2. Si el cambio fue exitoso, actualizamos el sello de seguridad.
                    // Esto invalida cualquier token JWT antiguo que use el sello de seguridad.
                    await userManager.UpdateSecurityStampAsync(identityUser);
                    userLogger.LogInformation("Contraseña actualizada con éxito para el usuario: {UserId}", userId);
                    return Results.Ok(Results.Ok(response));
                }

                // 3. Fallo: Suele ser por contraseña actual incorrecta o nueva contraseña no válida.
                userLogger.LogWarning("Fallo al cambiar la contraseña para el usuario: {UserId}. Errores: {Errors}", userId, string.Join(", ", result.Errors.Select(e => e.Description)));
                return Results.BadRequest(new CreateEditRemoveResponseDto { Success = false, Errors = result.Errors.Select(e => e.Description).ToList() });

            })
            .WithName("ChangePassword");


            // --- 5. DELETE /api/users/me (Eliminar Cuenta) ---
            group.MapDelete("/me", async (
                ClaimsPrincipal user,
                UserManager<IdentityUser> userManager) =>
            {
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

                var identityUser = await userManager.FindByIdAsync(userId);
                if (identityUser == null)
                {
                    return Results.NotFound(new GenericErrorDto("Usuario no encontrado.", "User.Delete"));
                }

                // 1. Eliminar el usuario y todos sus datos relacionados (IdentityUser y AspNetUserLogins, etc.)
                var result = await userManager.DeleteAsync(identityUser);

                if (result.Succeeded)
                {
                    userLogger.LogInformation("Usuario eliminado con éxito: {UserId}", userId);
                    // Nota: Después de esto, el token JWT del usuario seguirá siendo válido hasta que expire, 
                    // pero no podrá acceder a ningún recurso protegido que intente buscar su ID en la BD.
                    return Results.Ok(Results.Ok(response));
                }

                userLogger.LogError("Fallo al eliminar el usuario: {UserId}. Errores: {Errors}", userId, string.Join(", ", result.Errors.Select(e => e.Description)));
                return Results.BadRequest(new CreateEditRemoveResponseDto { Success = false, Errors = result.Errors.Select(e => e.Description).ToList() });

            })
            .WithName("DeleteUser");

            return app;
        }
    }
}