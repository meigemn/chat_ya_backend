using System.ComponentModel.DataAnnotations.Schema;

namespace chat_ya_backend.Models.Entities
{
    public class UserRoom
    {
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; } = default!;

        public int RoomId { get; set; }

        [ForeignKey(nameof(RoomId))]
        public ChatRoom Room { get; set; } = default!;
    }
}
