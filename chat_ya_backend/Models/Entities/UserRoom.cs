using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace chat_ya_backend.Models.Entities
{
    // Esta clase representa la relación Muchos a Muchos entre Usuarios y Salas.
    // Su clave primaria será compuesta por UserId y RoomId, definida en el DbContext.
    public class UserRoom
    {
        // Clave Foránea 1: El ID del usuario (de IdentityUser)
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public IdentityUser User { get; set; } = default!;

        // Clave Foránea 2: El ID de la sala (de ChatRoom)
        public int RoomId { get; set; }

        [ForeignKey(nameof(RoomId))]
        public ChatRoom Room { get; set; } = default!;
    }
}