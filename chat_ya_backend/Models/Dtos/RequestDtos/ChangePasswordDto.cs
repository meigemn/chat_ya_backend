using System.ComponentModel.DataAnnotations;

namespace chat_ya_backend.Models.Dtos.RequestDtos
{
    public class ChangePasswordDto
    {
        [Required]
        [MinLength(4)]
        public string CurrentPassword {  get; set; } 
        [MinLength(4)]
        [Required]
        public string NewPassword { get; set; } 
    }
}
