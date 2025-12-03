using System.ComponentModel.DataAnnotations;

namespace chat_ya_backend.Models.Dtos.UpdateDto
{
    public class UpdateMessageDto
    {
        [Required]
        [MinLength(1)]
        public string NewContent { get; set; } = string.Empty;
    }
}
