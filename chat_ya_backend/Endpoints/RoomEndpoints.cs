using chat_ya_backend.Models.Context;
using chat_ya_backend.Models.Dtos.CreateDtos;
using chat_ya_backend.Models.Dtos.EntityDtos;
using chat_ya_backend.Models.Dtos.UpdateDto;
using chat_ya_backend.Models.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace chat_ya_backend.Endpoints
{
    public static class RoomEndpoints
    {
        public static WebApplication MapRoomEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/rooms")
                           .RequireAuthorization()
                           .WithOpenApi()
                           .WithTags("ChatRooms");

            #region Get
            // --- 1. GET /api/rooms (Listar todas las salas del usuario) ---
            group.MapGet("/", async (
                ClaimsPrincipal user,
                MeigemnDbContext context,
                ILogger<object> roomLogger) =>
            {
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    roomLogger.LogWarning("Intento de listar salas sin userId en el token.");
                    return Results.Unauthorized();
                }

                var userRooms = await context.UserRooms
                    .Where(ur => ur.UserId == userId)
                    .Include(ur => ur.Room)
                    .ToListAsync();

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
            #endregion

            #region Post
            // --- 2. POST /api/rooms (Crear una nueva sala) ---
            group.MapPost("/", async (
                CreateRoomDto model,
                ClaimsPrincipal user,
                MeigemnDbContext context,
                UserManager<ApplicationUser> userManager,
                ILogger<object> roomLogger) =>
            {
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

                if (string.IsNullOrWhiteSpace(model.ChatRoomName))
                {
                    return Results.BadRequest(new { Error = "El nombre de la sala es obligatorio." });
                }

                var newRoom = new ChatRoom
                {
                    ChatRoomName = model.ChatRoomName,
                };

                context.ChatRoom.Add(newRoom);
                await context.SaveChangesAsync();

                var userRoomEntry = new UserRoom
                {
                    UserId = userId,
                    RoomId = newRoom.Id
                };

                context.UserRooms.Add(userRoomEntry);
                await context.SaveChangesAsync();

                var roomDto = new ChatRoomDto
                {
                    Id = newRoom.Id,
                    ChatRoomName = newRoom.ChatRoomName,
                };

                roomLogger.LogInformation("Sala creada con éxito. ID: {RoomId}, Creador: {UserId}", newRoom.Id, userId);
                return Results.Created($"/api/rooms/{newRoom.Id}", roomDto);
            })
            .WithName("CreateRoom");
            #endregion

            #region Put
            // --- 3. PUT /api/rooms/{id} (Actualizar el nombre de la sala) ---
            group.MapPut("/{id:int}", async (
                int id,
                UpdateChatRoomDto model,
                ClaimsPrincipal user,
                MeigemnDbContext context,
                ILogger<object> roomLogger) =>
            {
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

                if (string.IsNullOrWhiteSpace(model.ChatRoomName))
                {
                    return Results.BadRequest(new { Error = "El nombre de la sala no puede estar vacío." });
                }

                var isMember = await context.UserRooms
                    .AnyAsync(ur => ur.RoomId == id && ur.UserId == userId);

                if (!isMember)
                {
                    roomLogger.LogWarning("Usuario {UserId} intentó actualizar sala {RoomId} sin ser miembro.", userId, id);
                    return Results.Forbid();
                }

                var room = await context.ChatRoom.FindAsync(id);
                if (room == null)
                {
                    return Results.NotFound(new { Error = $"Sala con ID {id} no encontrada." });
                }

                room.ChatRoomName = model.ChatRoomName;
                await context.SaveChangesAsync();

                roomLogger.LogInformation("Sala {RoomId} actualizada por {UserId}.", id, userId);

                var roomDto = new ChatRoomDto
                {
                    Id = room.Id,
                    ChatRoomName = room.ChatRoomName,
                };

                return Results.Ok(roomDto);
            })
            .WithName("UpdateRoom");
            #endregion

            #region Delete
            // --- 4. DELETE /api/rooms/{id} (Eliminar la sala) ---
            group.MapDelete("/{id:int}", async (
                int id,
                ClaimsPrincipal user,
                MeigemnDbContext context,
                ILogger<object> roomLogger) =>
            {
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

                var userRoomEntry = await context.UserRooms
                    .FirstOrDefaultAsync(ur => ur.RoomId == id && ur.UserId == userId);

                if (userRoomEntry == null)
                {
                    roomLogger.LogWarning("Usuario {UserId} intentó eliminar sala {RoomId} sin ser miembro.", userId, id);
                    return Results.Forbid();
                }

                var roomToDelete = await context.ChatRoom.FindAsync(id);
                if (roomToDelete == null)
                {
                    return Results.NotFound(new { Error = $"Sala con ID {id} no encontrada." });
                }

                var allRoomParticipants = await context.UserRooms
                    .Where(ur => ur.RoomId == id)
                    .ToListAsync();

                context.UserRooms.RemoveRange(allRoomParticipants);

                context.ChatRoom.Remove(roomToDelete);
                await context.SaveChangesAsync();

                roomLogger.LogInformation("Sala {RoomId} y {Count} participantes eliminados por {UserId}.", id, allRoomParticipants.Count, userId);
                return Results.NoContent();
            })
            .WithName("DeleteRoom");
            #endregion

            return app;
        }
    }
}
