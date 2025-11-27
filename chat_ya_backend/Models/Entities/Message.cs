using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace chat_ya_backend.Models.Entities
{
    public class Message
    {
        [Key]
        public int Id { get; set; } // Clave Principal

        [Required]
        public string Content { get; set; } = string.Empty; // El contenido del mensaje

        public DateTime SentDate { get; set; } = DateTime.UtcNow; // Marca de tiempo

        // --- Claves Foráneas ---

        // FK al usuario (IdentityUser)
        [Required]
        public string SenderId { get; set; } = string.Empty;

        [ForeignKey(nameof(SenderId))]
        public IdentityUser Sender { get; set; } = default!; // Propiedad de Navegación del Usuario

        // FK a la sala de chat (ChatRoom)
        public int RoomId { get; set; }

        [ForeignKey(nameof(RoomId))]
        public ChatRoom Room { get; set; } = default!; // Propiedad de Navegación de la Sala
    }
}