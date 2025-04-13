namespace ProjectX.Hubs;

public class ConnectionMapping<T> where T : notnull
{
    private readonly Dictionary<T, HashSet<string>> _connections = new();

    public void Add(T key, string connectionId)
    {
        lock (_connections)
        {
            if (!_connections.TryGetValue(key, out var connections))
            {
                connections = new HashSet<string>();
                _connections[key] = connections;
            }

            connections.Add(connectionId);
        }
    }

    public void Remove(T key, string connectionId)
    {
        lock (_connections)
        {
            if (!_connections.TryGetValue(key, out var connections)) return;

            connections.Remove(connectionId);
            if (connections.Count == 0)
                _connections.Remove(key);
        }
    }

    public IReadOnlyCollection<string> GetConnections(T key)
    {
        lock (_connections)
        {
            return _connections.TryGetValue(key, out var connections)
                ? connections.ToList()
                : Array.Empty<string>();
        }
    }
}