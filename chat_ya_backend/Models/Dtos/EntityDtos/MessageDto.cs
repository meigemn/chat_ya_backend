namespace chat_ya_backend.Models.Dtos.EntityDtos
{
    public class MessageDto
    {
        public int Id { get; set; } 
        public string Content { get; set; } = string.Empty; 
        public DateTime SentDate { get; set; } 

        // Datos del remitente
        public string SenderId { get; set; } = string.Empty; 
        public string SenderUserName { get; set; } = string.Empty; 
        public int RoomId { get; set; } 
    }
}
