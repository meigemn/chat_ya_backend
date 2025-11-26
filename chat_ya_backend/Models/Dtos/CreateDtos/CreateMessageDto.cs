using System.ComponentModel.DataAnnotations;

namespace chat_ya_backend.Models.Dtos.CreateDtos
{
    public class CreateMessageDto
    {
        [Required]
        public string Body { get; set; } = string.Empty;
    }
}
