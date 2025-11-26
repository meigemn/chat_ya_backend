namespace chat_ya_backend.Models.Dtos.EntityDtos
{
    public class UserDto
    {
        // ID generado por Identity
        public string Id { get; set; } = string.Empty;

        // Nombre de usuario para mostrar
        public string Username { get; set; } = string.Empty;
    }
}
