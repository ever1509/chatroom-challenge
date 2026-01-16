using ChatRoom.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ChatRoom.Api.Data;

public class ChatDbContext: IdentityDbContext<IdentityUser>
{
    public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options)
    {
    }
    
    public DbSet<Room> ChatRooms => Set<Room>();
    public DbSet<Message> ChatMessages => Set<Message>();
     
}