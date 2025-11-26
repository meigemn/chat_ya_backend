namespace chat_ya_backend.Models.Dtos.ErrorDtos
{
    public class GenericErrorDto
    {
        
        public string Location { get; set; }
        public string Description { get; set; }

        public GenericErrorDto()
        { 
            Location = string.Empty;
            Description = string.Empty;
        }

        // Requerido  por el OrderController
        public GenericErrorDto( string description, string location = "OrderController")
        {
            
            Description = description;
            Location = location;
        }
    }
}
