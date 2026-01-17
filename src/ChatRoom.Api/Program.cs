using ChatRoom.Api.Data;
using ChatRoom.Api.Hubs;
using ChatRoom.Api.Models;
using ChatRoom.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ChatDbContext>(opt => opt.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity endpoints (register/login/logout)
builder.Services
    .AddIdentityApiEndpoints<IdentityUser>()
    .AddEntityFrameworkStores<ChatDbContext>();

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();

    
// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//SignalR
builder.Services.AddSignalR();

// RabbitMQ connection factory
builder.Services.AddSingleton<RabbitMqConnectionFactory>();
// Stock command publisher
builder.Services.AddSingleton<StockCommandPublisher>();

var app = builder.Build();

//Initialize database with default chat room
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
    if (!db.ChatRooms.Any())
    {
        db.ChatRooms.Add(new Room { Name = "General" });
        db.SaveChanges();
    }
}


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// maps /register, /login, /logout, etc.
// Use /login?useCookies=true for cookie-based auth (used by React app and SignalR)
app.MapIdentityApi<IdentityUser>();

// Endpoint to get info about the current user
app.MapGet("/api/me", (HttpContext ctx) 
    => Results.Ok(
        new
        {
            user = ctx.User.Identity?.Name, authenticated = ctx.User.Identity?.IsAuthenticated 
        })).RequireAuthorization();

// SignalR hub endpoint
app.MapHub<ChatHub>("/hubs/chat");

app.MapGet("/api/rooms", async (ChatDbContext db) =>
{
    var rooms = await db.ChatRooms
        .OrderBy(r => r.Name)
        .Select(r => new { r.Id, r.Name })
        .ToListAsync();

    return Results.Ok(rooms);
}).RequireAuthorization();

// Endpoint to create a new chat room
app.MapPost("/api/rooms", async (ChatDbContext db, Room room) =>
{
    var name = (room.Name ?? "").Trim();
    if (string.IsNullOrWhiteSpace(name))
        return Results.BadRequest("Room name is required.");

    // Optional: prevent duplicates
    var exists = await db.ChatRooms.AnyAsync(r => r.Name.ToLower() == name.ToLower());
    if (exists) return Results.Conflict("Room already exists.");

    var created = new Room { Name = name };
    db.ChatRooms.Add(created);
    await db.SaveChangesAsync();

    return Results.Ok(new { created.Id, created.Name });
}).RequireAuthorization();

//Endpoint to get last 50 messages from a chat room
app.MapGet("/api/rooms/{roomId:int}/messages", async (int roomId, ChatDbContext db) =>
{
    var msgs = await db.ChatMessages
        .Where(m => m.RoomId == roomId)
        .OrderByDescending(m => m.TimeStamp)
        .Take(50)
        .OrderBy(m => m.TimeStamp)
        .Select(m => new { m.Id, m.RoomId, m.UserName, m.Text, m.TimeStamp, m.IsBot })
        .ToListAsync();

    return Results.Ok(msgs);
}).RequireAuthorization();

app.Run();
