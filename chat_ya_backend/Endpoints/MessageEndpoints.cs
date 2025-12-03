using chat_ya_backend.Models.Context;
using chat_ya_backend.Models.Dtos.CreateDtos;
using chat_ya_backend.Models.Dtos.EntityDtos;
using chat_ya_backend.Models.Dtos.UpdateDto;
using chat_ya_backend.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace chat_ya_backend.Endpoints
{
    public static class MessageEndpoints
    {
        public static WebApplication MapMessageEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/messages")
                           .RequireAuthorization()
                           .WithOpenApi()
                           .WithTags("Mensajes");



            #region Get (mensajes en la sala)
            // --- GET /api/messages/room/{roomId} (Listar mensajes de una sala) ---
            group.MapGet("/room/{roomId:int}", async (
                int roomId,
                ClaimsPrincipal user,
                MeigemnDbContext context,
                ILogger<object> logger) =>
            {
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

                // 1. Verificar si el usuario es miembro de la sala (solo miembros pueden leer)
                var isMember = await context.UserRooms
                    .AnyAsync(ur => ur.RoomId == roomId && ur.UserId == userId);

                if (!isMember)
                {
                    return Results.Forbid();
                }

                // 2. Obtener los mensajes de la sala, incluyendo el usuario remitente
                var messages = await context.Messages
                    .Where(m => m.RoomId == roomId)
                    .OrderBy(m => m.SentDate) // Ordenar por fecha para el chat
                                              // Incluimos el Sender (IdentityUser) para obtener el UserName
                    .Include(m => m.Sender)
                    .ToListAsync();

                // 3. Mapear a DTOs
                var messageDtos = messages.Select(m => new MessageDto
                {
                    Id = m.Id,
                    Content = m.Content,
                    SentDate = m.SentDate,
                    RoomId = m.RoomId,
                    SenderId = m.SenderId,
                    SenderUserName = m.Sender.UserName ?? "Desconocido"
                }).ToList();

                return Results.Ok(messageDtos);
            })
            .WithName("GetRoomMessages");


            #endregion

            #region Get (mensajes enviados por el usuario)

            // --- GET /api/messages/me (Listar todos los mensajes enviados por el usuario) ---
            group.MapGet("/me", async (
                ClaimsPrincipal user,
                MeigemnDbContext context,
                ILogger<object> logger) =>
            {
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    logger.LogWarning("Intento de listar mensajes propios sin userId en el token.");
                    return Results.Unauthorized();
                }

                // 1. Obtener los mensajes donde el SenderId coincide con el userId
                var myMessages = await context.Messages
                    .Where(m => m.SenderId == userId)
                    .OrderByDescending(m => m.SentDate) // Ordenar por fecha para ver los más recientes   
                    .Include(m => m.Sender)// Incluir el Sender (IdentityUser) para obtener el UserName
                    .ToListAsync();

                // 2. Mapear a DTOs
                var messageDtos = myMessages.Select(m => new MessageDto
                {
                    Id = m.Id,
                    Content = m.Content,
                    SentDate = m.SentDate,
                    RoomId = m.RoomId,
                    SenderId = m.SenderId,
                    // Usamos el UserName de la entidad Sender incluida
                    SenderUserName = m.Sender.UserName ?? "Yo"
                }).ToList();

                logger.LogInformation("Usuario {UserId} consultó {Count} mensajes propios.", userId, messageDtos.Count);

                return Results.Ok(messageDtos);
            })
            .WithName("GetUserSentMessages");
            #endregion

            #region Post
            // --- POST /api/messages (Enviar un nuevo mensaje) ---
            group.MapPost("/", async (
                CreateMessageDto model,
                ClaimsPrincipal user,
                MeigemnDbContext context,
                ILogger<object> logger) =>
            {
                var senderId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(senderId)) return Results.Unauthorized();

                // 1. Verificar si el usuario es miembro de la sala
                var isMember = await context.UserRooms
                    .AnyAsync(ur => ur.RoomId == model.RoomId && ur.UserId == senderId);

                if (!isMember)
                {
                    logger.LogWarning("Usuario {SenderId} intentó enviar mensaje a sala {RoomId} sin ser miembro.", senderId, model.RoomId);
                    return Results.Forbid();
                }

                // 2. Crear la entidad Message
                var newMessage = new Message
                {
                    Content = model.Content,
                    SenderId = senderId,
                    RoomId = model.RoomId,
                    SentDate = DateTime.UtcNow // Se usa el valor por defecto de la entidad
                };

                context.Messages.Add(newMessage);
                await context.SaveChangesAsync();

                // 3. Mapear y devolver el DTO
                var senderUser = await context.Users.FindAsync(senderId);
                var messageDto = new MessageDto
                {
                    Id = newMessage.Id,
                    Content = newMessage.Content,
                    SentDate = newMessage.SentDate,
                    RoomId = newMessage.RoomId,
                    SenderId = senderId,
                    SenderUserName = senderUser?.UserName ?? "Desconocido"
                };

                logger.LogInformation("Mensaje {MessageId} enviado por {SenderId} a sala {RoomId}.", newMessage.Id, senderId, model.RoomId);

                return Results.Created($"/api/messages/{newMessage.Id}", messageDto);
            })
            .WithName("CreateMessage");

            #endregion

            #region Put
            // ---PUT /api/messages/{id} (Editar un mensaje) ---
            group.MapPut("/{id:int}", async (
                int id,
                UpdateMessageDto model,
                ClaimsPrincipal user,
                MeigemnDbContext context,
                ILogger<object> logger) =>
            {
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

                // 1. Buscar el mensaje y verificar que el usuario sea el remitente original
                var message = await context.Messages.FindAsync(id);

                if (message == null)
                {
                    return Results.NotFound(new { Error = "Mensaje no encontrado." });
                }

                if (message.SenderId != userId)
                {
                    // Solo el remitente puede editar su propio mensaje
                    logger.LogWarning("Usuario {UserId} intentó editar mensaje {MessageId} de otro usuario.", userId, id);
                    return Results.Forbid();
                }

                // 2. Actualizar el contenido
                message.Content = model.NewContent;
                // Opcional: Podrías agregar un campo "EditedDate" a tu entidad si lo deseas.

                await context.SaveChangesAsync();

                // 3. Devolver la versión actualizada
                var senderUser = await context.Users.FindAsync(userId);
                var messageDto = new MessageDto
                {
                    Id = message.Id,
                    Content = message.Content,
                    SentDate = message.SentDate,
                    RoomId = message.RoomId,
                    SenderId = message.SenderId,
                    SenderUserName = senderUser?.UserName ?? "Desconocido"
                };

                logger.LogInformation("Mensaje {MessageId} editado por {UserId}.", id, userId);

                return Results.Ok(messageDto);
            })
            .WithName("UpdateMessage");
            #endregion

            #region Delete
            // ---DELETE /api/messages/{id} (Eliminar un mensaje) ---
            group.MapDelete("/{id:int}", async (
                int id,
                ClaimsPrincipal user,
                MeigemnDbContext context,
                ILogger<object> logger) =>
            {
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

                // 1. Buscar el mensaje
                var message = await context.Messages.FindAsync(id);

                if (message == null)
                {
                    return Results.NotFound(new { Error = "Mensaje no encontrado." });
                }

                // 2. Verificar que el usuario sea el remitente original (o un administrador si tuvieras roles)
                if (message.SenderId != userId)
                {
                    logger.LogWarning("Usuario {UserId} intentó eliminar mensaje {MessageId} de otro usuario.", userId, id);
                    return Results.Forbid();
                }

                // 3. Eliminar y guardar cambios
                context.Messages.Remove(message);
                await context.SaveChangesAsync();

                logger.LogInformation("Mensaje {MessageId} eliminado por {UserId}.", id, userId);

                return Results.NoContent();
            })
            .WithName("DeleteMessage");
            #endregion

            return app;
        }
    }
}