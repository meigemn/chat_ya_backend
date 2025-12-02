using chat_ya_backend.Models.Context;
using chat_ya_backend.Models.Dtos.CreateDtos;
using chat_ya_backend.Models.Entities;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using chat_ya_backend.Models.Dtos.EntityDtos;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using chat_ya_backend.Models.Dtos.UpdateDto; // Necesario para UpdateChartRoomDto
using Microsoft.AspNetCore.Http.HttpResults;

namespace chat_ya_backend.Endpoints
{
    public static class RoomEndpoints
    {
        // Usamos ILogger<RoomEndpoints> para la inyección de logger. 
        
        public static WebApplication MapRoomEndpoints(this WebApplication app)
        {
            // Creamos un grupo de rutas base /api/rooms. 
            var group = app.MapGroup("/api/rooms")
                           .RequireAuthorization()
                           .WithOpenApi()
                           .WithTags("ChatRooms");

            #region Post
            // --- 1. POST /api/rooms (Crear una nueva sala) ---
            group.MapPost("/", async (
                CreateRoomDto model,
                ClaimsPrincipal user,
                MeigemnDbContext context,
                UserManager<IdentityUser> userManager,
                ILogger<object> roomLogger) => // Logger Inyectado
            {
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

                if (string.IsNullOrWhiteSpace(model.ChatRoomName))
                {
                    return Results.BadRequest(new { Error = "El nombre de la sala es obligatorio." });
                }

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
            #endregion
            #region Get
            // --- 2. GET /api/rooms (Listar todas las salas del usuario) ---
            group.MapGet("/", async (
                ClaimsPrincipal user,
                MeigemnDbContext context,
                ILogger<object> roomLogger) => // Logger Inyectado
            {
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    roomLogger.LogWarning("Intento de listar salas sin userId en el token.");
                    return Results.Unauthorized();
                }

                // 1. Obtiene las salas del usuario mediante la tabla de unión UserRoom
                var userRooms = await context.UserRooms
                    .Where(ur => ur.UserId == userId)
                    // 2. Incluye los datos de la sala de chat relacionada
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
            #endregion
            #region Put
            // 3. PUT /api/rooms/{id} (Actualizar el nombre de la sala) ---
            group.MapPut("/{id:int}", async (
                int id,
                UpdateChartRoomDto model,
                ClaimsPrincipal user,
                MeigemnDbContext context,
                ILogger<object> roomLogger) => // Logger Inyectado
            {
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

                if (string.IsNullOrWhiteSpace(model.ChatRoomName))
                {
                    return Results.BadRequest(new { Error = "El nombre de la sala no puede estar vacío." });
                }

                // 1. Verificar si el usuario pertenece a la sala (solo los miembros pueden actualizar)
                var isMember = await context.UserRooms
                    .AnyAsync(ur => ur.RoomId == id && ur.UserId == userId);

                if (!isMember)
                {
                    roomLogger.LogWarning("Usuario {UserId} intentó actualizar sala {RoomId} sin ser miembro.", userId, id);
                    return Results.Forbid(); // 403 Forbidden o Unauthorized
                }

                // 2. Buscar la sala
                var room = await context.ChatRoom.FindAsync(id);
                if (room == null)
                {
                    return Results.NotFound(new { Error = $"Sala con ID {id} no encontrada." });
                }

                // 3. Actualizar
                room.ChatRoomName = model.ChatRoomName;
                await context.SaveChangesAsync();

                roomLogger.LogInformation("Sala {RoomId} actualizada por {UserId}.", id, userId);

                // 4. Devolver la sala actualizada
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
            // DELETE /api/rooms/{id} (Eliminar la sala) ---
            group.MapDelete("/{id:int}", async (
                int id,
                ClaimsPrincipal user,
                MeigemnDbContext context,
                ILogger<object> roomLogger) => // Logger Inyectado
            {
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

                // 1. Verificar si el usuario pertenece a la sala (solo los miembros o administradores pueden eliminar
                // Si tu modelo tiene roles (admin/miembro), debes revisar el rol.
                var userRoomEntry = await context.UserRooms
                    .FirstOrDefaultAsync(ur => ur.RoomId == id && ur.UserId == userId);

                if (userRoomEntry == null)
                {
                    roomLogger.LogWarning("Usuario {UserId} intentó eliminar sala {RoomId} sin ser miembro.", userId, id);
                    return Results.Forbid();
                }

                // 2. Buscar la sala (ya que sabemos que existe si userRoomEntry no es nulo)
                var roomToDelete = await context.ChatRoom.FindAsync(id);
                if (roomToDelete == null)
                {
                    // Esto no debería pasar si UserRoom existe, pero lo dejo por seguridad
                    return Results.NotFound(new { Error = $"Sala con ID {id} no encontrada." });
                }

                // 3. Eliminar primero todas las entradas de UserRoom asociadas a esta 
                var allRoomParticipants = await context.UserRooms
                    .Where(ur => ur.RoomId == id)
                    .ToListAsync();

                context.UserRooms.RemoveRange(allRoomParticipants);

                // Opcional: Si tienes mensajes, debes eliminarlos aquí también:
                // var roomMessages = await context.Messages.Where(m => m.ChatRoomId == id).ToListAsync();
                // context.Messages.RemoveRange(roomMessages);

                // 4. Eliminar la sala de chat
                context.ChatRoom.Remove(roomToDelete);

                await context.SaveChangesAsync();

                roomLogger.LogInformation("Sala {RoomId} y {Count} participantes eliminados por {UserId}.", id, allRoomParticipants.Count, userId);

                return Results.NoContent(); // 204 No Content: éxito sin cuerpo de respuesta.
            })
            .WithName("DeleteRoom");
            #endregion
            return app;
        }
    }
}