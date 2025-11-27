using chat_ya_backend.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq; // Aunque no es necesario aquí, lo mantengo de tu código

namespace chat_ya_backend.Models.Context
{
    // Hereda de IdentityDbContext, usando la clase base 'IdentityUser'
    public class MeigemnDbContext : IdentityDbContext<IdentityUser>
    {
    
        // Propiedades DbSet 

        public DbSet<ChatRoom> ChatRoom { get; set; } = default!;
        public DbSet<Message> Messages { get; set; } = default!;

        public DbSet<UserRoom> UserRooms { get; set; } = default!;


        // Constructor necesario para pasar las opciones a la clase base (DbContext)
        public MeigemnDbContext(DbContextOptions<MeigemnDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // 1. Llamada para configurar las tablas de Identity (AspNetUsers, AspNetRoles, etc.)
            base.OnModelCreating(builder);

            // CONFIGURACIÓN DE LAS ENTIDADES DE CHAT

            // 2. Configurar la clave compuesta para la tabla de unión UserRoom
            builder.Entity<UserRoom>()
                .HasKey(ur => new { ur.UserId, ur.RoomId });

            // 3. Configurar la relación de UserRoom con IdentityUser
            builder.Entity<UserRoom>()
                .HasOne(ur => ur.User)
                .WithMany() // <-- IdentityUser no tiene una colección 'UserRooms' por defecto
                .HasForeignKey(ur => ur.UserId);

            // Relación con ChatRoom 
            builder.Entity<UserRoom>()
                .HasOne(ur => ur.Room)
                .WithMany(r => r.UserRooms)
                .HasForeignKey(ur => ur.RoomId);
        }
    }
}