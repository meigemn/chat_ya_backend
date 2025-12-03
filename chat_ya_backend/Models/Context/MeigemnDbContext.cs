using chat_ya_backend.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace chat_ya_backend.Models.Context
{
    // Hereda de IdentityDbContext, usando la clase ApplicationUser
    public class MeigemnDbContext : IdentityDbContext<ApplicationUser>
    {

        // Propiedades DbSet 

        public DbSet<ChatRoom> ChatRoom { get; set; } = default!;
        public DbSet<Message> Messages { get; set; } = default!;

        public DbSet<UserRoom> UserRooms { get; set; } = default!;


        // Constructor
        public MeigemnDbContext(DbContextOptions<MeigemnDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // 1. Configuración base de Identity
            base.OnModelCreating(builder);

            // CONFIGURACIÓN DE LAS ENTIDADES DE CHAT
            // ----------------------------------------

            // 2. Configurar la clave compuesta para la tabla de unión UserRoom (Muchos a Muchos)
            builder.Entity<UserRoom>()
                .HasKey(ur => new { ur.UserId, ur.RoomId });

            // 3. Configurar la relación de UserRoom con ApplicationUser
            builder.Entity<UserRoom>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRooms) // 👈 Usa la propiedad de navegación en ApplicationUser
                .HasForeignKey(ur => ur.UserId);

            // 4. Relación UserRoom con ChatRoom
            builder.Entity<UserRoom>()
                .HasOne(ur => ur.Room)
                .WithMany(r => r.UserRooms)
                .HasForeignKey(ur => ur.RoomId);


            // CONFIGURACIÓN DE LOS MENSAJES
            // -----------------------------

            // 5. Relación Message con ChatRoom (Uno a Muchos: Sala tiene mensajes)
            builder.Entity<Message>()
                .HasOne(m => m.Room)
                .WithMany(r => r.Messages)
                .HasForeignKey(m => m.RoomId)
                .OnDelete(DeleteBehavior.Cascade); // Si la sala se elimina, los mensajes se eliminan (Cascade)

            // 6. Relación Message con ApplicationUser (Uno a Muchos: Usuario tiene mensajes)
            builder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany(u => u.Messages) // Usa la propiedad de navegación en ApplicationUser
                .HasForeignKey(m => m.SenderId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict); // Evita eliminar un usuario si tiene mensajes (Restrict)
        }
    }
}