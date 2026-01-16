using System.Security.Claims;
using ChatRoom.Api.Data;
using ChatRoom.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ChatRoom.Api.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly ChatDbContext _db;

    public ChatHub(ChatDbContext db)
    {
        _db = db;
    }
    
    private static string GroupName(int roomId) => $"room:{roomId}";

    public async Task SendMessage(int roomId, string message)
    {
        message = (message ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(message)) return;
        
        var roomExists = await _db.ChatRooms.AnyAsync(r => r.Id == roomId);
        if (!roomExists) throw new HubException("Room not found");

        var user = Context.User!;
        var userId = user.FindFirstValue("sub") 
                     ?? user.FindFirstValue(ClaimTypes.NameIdentifier) 
                     ?? string.Empty;
        
        var userName = user.Identity?.Name ?? "unknown";
        
        if(message.Length > 500) throw new HubException("Message too long (max 500 chars)");

        var msg = new Message
        {
            RoomId = roomId,
            UserId = userId,
            UserName = userName,
            Text = message,
            TimeStamp = DateTime.UtcNow,
            IsBot = false
        };
        
        _db.ChatMessages.Add(msg);
        await _db.SaveChangesAsync();
        
        await Clients.Group(GroupName(roomId)).SendAsync("newMessage", new
        {
            msg.Id,
            msg.RoomId,
            msg.UserName,
            msg.Text,
            msg.TimeStamp,
            msg.IsBot
        });
    }

    public async Task JoinRoom(int roomId)
    {
        var exists = await _db.ChatRooms.AnyAsync(r => r.Id == roomId);
        if(!exists) throw new HubException("Room not found");
        
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(roomId));
        await Clients.Group(GroupName(roomId)).SendAsync("UserJoined", Context.User?.Identity?.Name);
    }

    public async Task LeaveRoom(int roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(roomId));
        await Clients.Group(GroupName(roomId)).SendAsync("UserLeft", Context.User?.Identity?.Name);
    }
}