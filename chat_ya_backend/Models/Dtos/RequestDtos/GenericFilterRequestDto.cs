using System.ComponentModel;

namespace chat_ya_backend.Models.Dtos.RequestDtos
{
    public class GenericFilterRequestDto
    {
        // Propiedades de filtrado genérico (las mantienes)
        public object? Value { get; set; }

       

        public string? PropertyName { get; set; }

        // Cadena de búsqueda general (la mantienes)
        public string? SearchString { get; set; }

       
    }
}
