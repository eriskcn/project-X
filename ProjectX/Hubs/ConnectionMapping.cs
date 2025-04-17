using System.Collections.Concurrent;

namespace ProjectX.Hubs;

public class ConnectionMapping<T>(ILogger<ConnectionMapping<T>> logger, bool enableDebugLogging = true)
    where T : notnull
{
    private readonly ConcurrentDictionary<T, ConcurrentDictionary<string, bool>> _connections = new();
    private readonly ILogger<ConnectionMapping<T>> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public void Add(T key, string connectionId)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (string.IsNullOrEmpty(connectionId)) throw new ArgumentNullException(nameof(connectionId));

        var connections = _connections.GetOrAdd(key, _ => new ConcurrentDictionary<string, bool>());
        if (connections.TryAdd(connectionId, true))
        {
            if (enableDebugLogging)
            {
                _logger.LogDebug("Added connection {ConnectionId} for key {Key}", connectionId, key);
            }
        }
        else
        {
            _logger.LogWarning("Failed to add connection {ConnectionId} for key {Key}. It may already exist.",
                connectionId, key);
        }
    }

    public void Remove(T key, string connectionId)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (string.IsNullOrEmpty(connectionId)) throw new ArgumentNullException(nameof(connectionId));

        if (!_connections.TryGetValue(key, out var connections))
        {
            if (enableDebugLogging)
            {
                _logger.LogDebug("No connections found for key {Key} when removing {ConnectionId}", key, connectionId);
            }

            return;
        }

        if (connections.TryRemove(connectionId, out _))
        {
            if (enableDebugLogging)
            {
                _logger.LogDebug("Removed connection {ConnectionId} for key {Key}", connectionId, key);
            }
        }

        if (connections.IsEmpty)
        {
            if (_connections.TryRemove(key, out _))
            {
                if (enableDebugLogging)
                {
                    _logger.LogDebug("Removed empty connection set for key {Key}", key);
                }
            }
            else
            {
                _logger.LogWarning("Failed to remove empty connection set for key {Key}", key);
            }
        }
    }

    public bool HasConnection(T key, string connectionId)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (string.IsNullOrEmpty(connectionId)) throw new ArgumentNullException(nameof(connectionId));

        return _connections.TryGetValue(key, out var connections) && connections.ContainsKey(connectionId);
    }

    public IReadOnlyCollection<string> GetConnections(T key)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));

        if (!_connections.TryGetValue(key, out var connections))
        {
            return Array.Empty<string>();
        }

        return connections.Keys.ToList().AsReadOnly();
    }

    public int Count => _connections.Count;

    public void Clear()
    {
        _connections.Clear();
        if (enableDebugLogging)
        {
            _logger.LogDebug("Cleared all connections");
        }
    }
}