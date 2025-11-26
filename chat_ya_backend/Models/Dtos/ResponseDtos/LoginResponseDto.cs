using chat_ya_backend.Models.Dtos.EntityDtos;

namespace chat_ya_backend.Models.Dtos.ResponseDtos
{
    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public UserDto User { get; set; } = new UserDto();
        public DateTime Expiration { get; set; } = DateTime.UtcNow.AddHours(1);
    }
}
