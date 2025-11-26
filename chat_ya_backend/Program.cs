#region Usings y Dependencias
using chat_ya_backend.Models.Context;
using chat_ya_backend.Models.Dtos.CreateDtos;
using chat_ya_backend.Models.Dtos.EntityDtos;
using chat_ya_backend.Models.Dtos.ErrorDtos;
using chat_ya_backend.Models.Dtos.RequestDtos;
using chat_ya_backend.Models.Dtos.ResponseDtos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http; // Necesario para StatusCodes
using Microsoft.Extensions.Logging; // Necesario para ILoggerFactory
#endregion

var builder = WebApplication.CreateBuilder(args);

#region 1. Configuración de Servicios (Servicios)

// 1.1. Configuración de Logging (Usando el logger por defecto de .NET)
// Se mantiene comentado el bloque Serilog, que era el origen del fallo silencioso.

// 1.2. Configuración de la Base de Datos (SQLite) y EF Core
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<MeigemnDbContext>(options =>
    options.UseSqlite(connectionString));

// 1.3. Configuración de ASP.NET Core Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    // Opciones de contraseña
    options.Password.RequiredLength = 4;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
})
.AddEntityFrameworkStores<MeigemnDbContext>() // Vincula Identity con tu DbContext
.AddDefaultTokenProviders();

// 1.4. Configuración de Autenticación JWT (Bearer Token)
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key no configurada.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer no configurado.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

// 1.5. Autorización
builder.Services.AddAuthorization();

// 1.6. Configuración de SwaggerGen con Autenticación JWT
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // 1. Definición del Esquema de Seguridad (Bearer Token)
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Ingresa el token JWT en el formato: Bearer {token}"
    });

    // 2. Aplicar el Esquema de Seguridad a todos los Endpoints
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});
#endregion

var app = builder.Build();

#region 2. Configuración del Middleware HTTP (Pipeline)

// 2.1. Development Setup (Swagger)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 2.2. Middleware de Seguridad: Autenticación y Autorización
app.UseAuthentication();
app.UseAuthorization();

// 2.3. Endpoint de prueba para verificar que el token funciona
app.MapGet("/api/chat/status", (ClaimsPrincipal user) =>
{
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    return Results.Ok($"Bienvenido al chat, {user.Identity?.Name}. Tu ID es: {userId}");
})
.RequireAuthorization()
.WithName("GetChatStatus")
.WithOpenApi();

#endregion

#region 3. Endpoints de Autenticación

// 3.1. Endpoint de Registro de Usuario
app.MapPost("/api/auth/register", async (CreateUserDto model, UserManager<IdentityUser> userManager, ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("AuthEndpoints");
    logger.LogInformation("Attempting to register new user: {Username}", model.Username);

    var user = new IdentityUser { UserName = model.Username, Email = model.Email };
    var result = await userManager.CreateAsync(user, model.Password);

    var response = new CreateEditRemoveResponseDto();

    if (result.Succeeded)
    {
        response.IsSuccess(0);
        logger.LogInformation("User created successfully: {Email}", model.Email);
        return Results.Ok(response);
    }

    // Respuesta de error (400 Bad Request)
    response.Success = false;
    response.Errors = result.Errors.Select(e => e.Description).ToList();

    logger.LogError("User registration failed for {Email}: {Errors}", model.Email, string.Join(", ", response.Errors));
    return Results.BadRequest(response);
})
.WithName("RegisterUser")
.WithOpenApi()
.AllowAnonymous();

// 3.2. Endpoint de Login
app.MapPost("/api/auth/login", async (
    LoginRequestDtos model,
    SignInManager<IdentityUser> signInManager,
    UserManager<IdentityUser> userManager,
    IConfiguration config,
    ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("AuthEndpoints");
    // 1. Verificación de Credenciales
    if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
    {
        return Results.BadRequest(new GenericErrorDto("Email y contraseña son requeridos.", "Auth.Login"));
    }

    var user = await userManager.FindByEmailAsync(model.Email);

    if (user == null)
    {
        logger.LogWarning("Login fallido: Usuario no encontrado para email {Email}", model.Email);
        // Usamos Results.Json con 401 Unauthorized
        return Results.Json(
            new GenericErrorDto("Credenciales inválidas.", "Auth.Login"),
            statusCode: StatusCodes.Status401Unauthorized
        );
    }

    var result = await signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: false);

    if (!result.Succeeded)
    {
        logger.LogWarning("Login fallido para el usuario {UserName} - Contraseña incorrecta.", user.UserName);
        // Usamos Results.Json con 401 Unauthorized
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
        User = new UserDto { Id = user.Id, Username = user.UserName! },
        Expiration = expiration
    };

    logger.LogInformation("Usuario {UserName} ha iniciado sesión con éxito.", user.UserName);
    return Results.Ok(response);
})
.WithName("LoginUser")
.WithOpenApi()
.AllowAnonymous();
#endregion

app.Run();