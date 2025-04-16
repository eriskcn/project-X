using Microsoft.AspNetCore.SignalR;

namespace ProjectX.Hubs;

public class NotificationHub(ConnectionMapping<Guid> connectionMapping) : Hub
{
    public override Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (Guid.TryParse(userId, out var parsedId))
        {
            connectionMapping.Add(parsedId, Context.ConnectionId);
        }

        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (Guid.TryParse(userId, out var parsedId))
        {
            connectionMapping.Remove(parsedId, Context.ConnectionId);
        }

        return base.OnDisconnectedAsync(exception);
    }
}