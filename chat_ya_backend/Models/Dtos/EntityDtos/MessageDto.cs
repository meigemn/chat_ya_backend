namespace chat_ya_backend.Models.Dtos.EntityDtos
{
    public class MessageDto
    {
        public int Id { get; set; } 
        public string Content { get; set; } 
        public DateTime SentDate { get; set; } 

        // Datos del remitente
        public string SenderId { get; set; }  
        public string SenderUserName { get; set; } 
        public int RoomId { get; set; } 
    }
}
