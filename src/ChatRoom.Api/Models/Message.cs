namespace ChatRoom.Api.Models;

public class Message
{
    public long Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime TimeStamp { get; set; } = DateTime.Now;
    public bool IsBot { get; set; } = false;
    
    public int RoomId { get; set; }
    public Room? Room { get; set; }
    
}