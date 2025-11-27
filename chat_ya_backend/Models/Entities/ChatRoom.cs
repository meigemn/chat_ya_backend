using System.ComponentModel.DataAnnotations;
using chat_ya_backend.Models.Entities;
namespace chat_ya_backend.Models.Entities
{
    public class ChatRoom
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string ChatRoomName { get; set; } = string.Empty;
        // Propiedad de Navegación: Mensajes que pertenecen a esta sala
        public ICollection<Message> Messages { get; set; } = new List<Message>();

        // Propiedad de Navegación: Usuarios que pertenecen a esta sala (a través de UserRoom)
        public ICollection<UserRoom> UserRooms { get; set; } = new List<UserRoom>();
    }
}
