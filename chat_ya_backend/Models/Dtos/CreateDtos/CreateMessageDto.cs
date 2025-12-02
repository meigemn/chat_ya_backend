using System.ComponentModel.DataAnnotations;

namespace chat_ya_backend.Models.Dtos.CreateDtos
{
    public class CreateMessageDto
    {
        [Required]
        public string Content { get; set; } = string.Empty; 

        [Required]
        public int RoomId { get; set; } 
    }
}
