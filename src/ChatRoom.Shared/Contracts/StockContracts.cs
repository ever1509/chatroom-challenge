namespace ChatRoom.Shared.Contracts;

public record StockCommand(
    string StockCode,
    int RoomId,
    string RequestedByUserName,
    string CorrelationId,
    DateTime TimeStamp
);

public record StockResult(
    int RoomId,
    string Text,
    string CorrelationId,
    DateTime TimeStamp
);