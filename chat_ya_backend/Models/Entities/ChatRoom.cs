using System.ComponentModel.DataAnnotations;

namespace chat_ya_backend.Models.Entities
{
    public class ChatRoom
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ChatRoomName { get; set; } = string.Empty;

        public ICollection<Message> Messages { get; set; } = new List<Message>();

        public ICollection<UserRoom> UserRooms { get; set; } = new List<UserRoom>();
    }
}
