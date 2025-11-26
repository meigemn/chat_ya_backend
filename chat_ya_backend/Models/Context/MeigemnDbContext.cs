using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace chat_ya_backend.Models.Context
{
    // Hereda de IdentityDbContext, usando la clase base 'IdentityUser'
    // Esto proporciona automáticamente todas las tablas (Users, Roles, Claims, etc.)
    // y las propiedades predeterminadas para los usuarios.
    public class MeigemnDbContext : IdentityDbContext<IdentityUser>
    {
        // Constructor necesario para pasar las opciones a la clase base (DbContext)
        public MeigemnDbContext(DbContextOptions<MeigemnDbContext> options)
            : base(options)
        {
        }
    }
}
