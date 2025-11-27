using chat_ya_backend.Models.Context;
using chat_ya_backend.Models.Dtos.CreateDtos;
using chat_ya_backend.Models.Entities;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using chat_ya_backend.Models.Dtos.EntityDtos;
using Microsoft.Extensions.Logging; // Necesario para ILoggerFactory
using Microsoft.EntityFrameworkCore; // Necesario para .Include y ToListAsync

namespace chat_ya_backend.Endpoints
{
    public static class RoomEndpoints
    {
        public static WebApplication MapRoomEndpoints(this WebApplication app)
        {
            // Creamos un grupo de rutas base /api/rooms. 

            var group = app.MapGroup("/api/rooms")
                           .RequireAuthorization()
                           .WithOpenApi()
                           .WithTags("ChatRooms");

            var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
            var roomLogger = loggerFactory.CreateLogger("RoomEndpoints");

            // --- 1. POST /api/rooms (Crear una nueva sala) ---
            group.MapPost("/", async (
                CreateRoomDto model,
                ClaimsPrincipal user,
                MeigemnDbContext context,
                UserManager<IdentityUser> userManager) =>
            {
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

                // 2. Crear la entidad ChatRoom
                var newRoom = new ChatRoom
                { 
                    ChatRoomName = model.ChatRoomName, 
                };

                context.ChatRoom.Add(newRoom);
                await context.SaveChangesAsync();

                // 3. Añadir al usuario creador a la tabla de unión UserRoom
                var userRoomEntry = new UserRoom
                {
                    UserId = userId,
                    RoomId = newRoom.Id
                };

                context.UserRooms.Add(userRoomEntry);
                await context.SaveChangesAsync();

                // 4. Mapear y devolver el DTO de respuesta
                var roomDto = new ChatRoomDto
                {
                    Id = newRoom.Id,
                    ChatRoomName = newRoom.ChatRoomName,
                };

                roomLogger.LogInformation("Sala creada con éxito. ID: {RoomId}, Creador: {UserId}", newRoom.Id, userId);

                return Results.Created($"/api/rooms/{newRoom.Id}", roomDto);
            })
            .WithName("CreateRoom");

            // --- 2. GET /api/rooms (Listar todas las salas del usuario) ---
            group.MapGet("/", async (
                ClaimsPrincipal user,
                MeigemnDbContext context) =>
            {
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    roomLogger.LogWarning("Intento de listar salas sin userId en el token.");
                    return Results.Unauthorized();
                }

                // 1. Obtener las salas del usuario mediante la tabla de unión UserRoom
                var userRooms = await context.UserRooms
                    .Where(ur => ur.UserId == userId)
                    // 2. Incluir los datos de la sala de chat relacionada
                    .Include(ur => ur.Room)
                    .ToListAsync();

                // 3. Mapear los resultados
                var roomsList = userRooms
                    .Select(ur => new ChatRoomDto
                    {
                        Id = ur.Room.Id,
                        ChatRoomName = ur.Room.ChatRoomName, 

                    })
                    .ToList();

                roomLogger.LogInformation("Usuario {UserId} consultó {Count} salas.", userId, roomsList.Count);

                return Results.Ok(roomsList);
            })
            .WithName("GetUserRooms");


            return app;
        }
    }
}