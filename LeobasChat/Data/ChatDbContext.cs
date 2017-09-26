using Microsoft.EntityFrameworkCore;

namespace LeobasChat.Data
{
    public class ChatDbContext : DbContext
    {
        public ChatDbContext(DbContextOptions options) : base(options)
        {

        }

        public DbSet<ChatRoom> ChatRooms { get; set; }

        public DbSet<ChatUser> ChatUsers { get; set; }
    }
}
