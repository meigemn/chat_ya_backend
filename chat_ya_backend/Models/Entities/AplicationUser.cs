using chat_ya_backend.Models.Entities;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace chat_ya_backend.Models.Entities
{
    public class ApplicationUser : IdentityUser
    {
        // Propiedad de navegación para los mensajes 
        public ICollection<Message> Messages { get; set; } = new List<Message>();

        // Propiedad de navegación para las salas (para relación Many-to-Many)
        public ICollection<UserRoom> UserRooms { get; set; } = new List<UserRoom>();
    }
}