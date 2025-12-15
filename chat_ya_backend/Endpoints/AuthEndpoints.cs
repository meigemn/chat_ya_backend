using chat_ya_backend.Models.Dtos.CreateDtos;
using chat_ya_backend.Models.Dtos.EntityDtos;
using chat_ya_backend.Models.Dtos.ErrorDtos;
using chat_ya_backend.Models.Dtos.RequestDtos;
using chat_ya_backend.Models.Dtos.ResponseDtos;
using chat_ya_backend.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
namespace chat_ya_backend.Endpoints
{
    /// <summary>
    /// Maneja la creacion de la identidad del usuario
    /// </summary>
    public static class AuthEndpoints
    {
        public static WebApplication MapAuthEndpoints(this WebApplication app)
        {
            // Creamos un grupo de rutas base /api/auth
            var group = app.MapGroup("/api/auth")
                .WithTags("Registro y Login");

            // Reutilizamos el Logger Factory de la aplicación
            var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
            var authLogger = loggerFactory.CreateLogger("AuthEndpoints");
            #region Endpoint registro de usuario
            // 1. Endpoint de Registro de Usuario
            group.MapPost("/register", async (CreateUserDto model, UserManager<ApplicationUser> userManager) =>
            {
                authLogger.LogInformation("Attempting to register new user: {Username}", model.Username);

                var user = new ApplicationUser { UserName = model.Username, Email = model.Email };
                var result = await userManager.CreateAsync(user, model.Password);

                var response = new CreateEditRemoveResponseDto();

                if (result.Succeeded)
                {
                    response.IsSuccess(0);
                    authLogger.LogInformation("User created successfully: {Email}", model.Email);
                    return Results.Ok(response);
                }

                // Respuesta de error (400 Bad Request)
                response.Success = false;
                response.Errors = result.Errors.Select(e => e.Description).ToList();

                authLogger.LogError("User registration failed for {Email}: {Errors}", model.Email, string.Join(", ", response.Errors));
                return Results.BadRequest(response);
            })
            .WithName("RegisterUser")
            .WithOpenApi()
            .AllowAnonymous();
            #endregion

            #region Endpoint de Login
            // 2. Endpoint de Login
            group.MapPost("/login", async (
                LoginRequestDtos model,
                SignInManager<ApplicationUser> signInManager,
                UserManager<ApplicationUser> userManager,
                IConfiguration config) =>
            {
                // 1. Verificación de Credenciales
                if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
                {
                    return Results.BadRequest(new GenericErrorDto("Email y contraseña son requeridos.", "Auth.Login"));
                }

                var user = await userManager.FindByEmailAsync(model.Email);

                if (user == null)
                {
                    authLogger.LogWarning("Login fallido: Usuario no encontrado para email {Email}", model.Email);
                    return Results.Json(
                        new GenericErrorDto("Credenciales inválidas.", "Auth.Login"),
                        statusCode: StatusCodes.Status401Unauthorized
                    );
                }

                var result = await signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: false);

                if (!result.Succeeded)
                {
                    authLogger.LogWarning("Login fallido para el usuario {UserName} - Contraseña incorrecta.", user.UserName);
                    return Results.Json(
                        new GenericErrorDto("Credenciales inválidas.", "Auth.Login"),
                        statusCode: StatusCodes.Status401Unauthorized
                    );
                }
                

                // 2. Generación del JWT
                var jwtKeyLocal = config["Jwt:Key"]!;
                var jwtIssuerLocal = config["Jwt:Issuer"]!;

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Name, user.UserName ?? user.Email!),
                    new Claim(ClaimTypes.Email, user.Email!)
                };

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKeyLocal));
                var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                var expiration = DateTime.UtcNow.AddHours(1);

                var token = new JwtSecurityToken(
                    issuer: jwtIssuerLocal,
                    audience: null,
                    claims: claims,
                    expires: expiration,
                    signingCredentials: credentials);

                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

                // 3. Devolver la Respuesta (200 OK)
                var response = new LoginResponseDto
                {
                    Token = tokenString,
                    User = new UserDto { Id = user.Id, UserName = user.UserName! },
                    Expiration = expiration
                };

                authLogger.LogInformation("Usuario {UserName} ha iniciado sesión con éxito.", user.UserName);
                return Results.Ok(response);
            })
            .WithName("LoginUser")
            .WithOpenApi()
            .AllowAnonymous();
            #endregion

            return app;
        }
    }
}
