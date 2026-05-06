using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;

// Packet type identifiers shared between server and client
public static class PacketType
{
    public const byte Position = 0;  // float x, float y
    public const byte Mine     = 1;  // int cellX, int cellY
}

/// <summary>
/// Authoritative relay server.
/// Bind this to one machine (or run it in the Editor).
/// It receives packets from any connected client and
/// forwards them to every OTHER connected client.
/// </summary>
public class NetworkServer : MonoBehaviour
{
    [Header("Config")]
    public ushort port = 9000;

    private NetworkDriver driver;
    private NativeList<NetworkConnection> connections;

    void Start()
    {
        driver = NetworkDriver.Create();
        connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);

        // Bind to all interfaces on the configured port
        var endpoint = NetworkEndpoint.AnyIpv4.WithPort(port);
        if (driver.Bind(endpoint) != 0)
        {
            Debug.LogError($"[Server] Failed to bind on port {port}.");
            return;
        }

        driver.Listen();
        Debug.Log($"[Server] Listening on port {port}.");
    }

    void Update()
    {
        // Flush outgoing and process incoming messages
        driver.ScheduleUpdate().Complete();

        // --- Accept new connections ---
        NetworkConnection incoming;
        while ((incoming = driver.Accept()) != default)
        {
            connections.Add(incoming);
            Debug.Log($"[Server] Client connected. Total: {connections.Length}");
        }

        // --- Process events for every known connection ---
        for (int i = connections.Length - 1; i >= 0; i--)
        {
            // Remove stale connections (disconnected clients)
            if (!connections[i].IsCreated)
            {
                connections.RemoveAtSwapBack(i);
                continue;
            }

            NetworkEvent.Type evt;
            while ((evt = driver.PopEventForConnection(connections[i], out var stream)) != NetworkEvent.Type.Empty)
            {
                switch (evt)
                {
                    case NetworkEvent.Type.Data:
                        // Read the raw packet and relay it to all OTHER clients
                        RelayToOthers(i, ref stream);
                        break;

                    case NetworkEvent.Type.Disconnect:
                        Debug.Log($"[Server] Client {i} disconnected.");
                        connections[i] = default;
                        break;
                }
            }
        }
    }

    // Reads the full packet from `stream` and forwards it verbatim to every
    // connection except the sender at index `senderIndex`.
    private void RelayToOthers(int senderIndex, ref DataStreamReader stream)
    {
        // Peek the packet type byte without consuming the stream position —
        // we re-read it below so the full packet stays intact for forwarding.
        byte packetType = stream.ReadByte();

        if (packetType == PacketType.Position)
        {
            float x = stream.ReadFloat();
            float y = stream.ReadFloat();

            for (int i = 0; i < connections.Length; i++)
            {
                if (i == senderIndex || !connections[i].IsCreated) continue;

                driver.BeginSend(connections[i], out var writer);
                writer.WriteByte(PacketType.Position);
                writer.WriteFloat(x);
                writer.WriteFloat(y);
                driver.EndSend(writer);
            }
        }
        else if (packetType == PacketType.Mine)
        {
            int cellX = stream.ReadInt();
            int cellY = stream.ReadInt();

            for (int i = 0; i < connections.Length; i++)
            {
                if (i == senderIndex || !connections[i].IsCreated) continue;

                driver.BeginSend(connections[i], out var writer);
                writer.WriteByte(PacketType.Mine);
                writer.WriteInt(cellX);
                writer.WriteInt(cellY);
                driver.EndSend(writer);
            }
        }
        else
        {
            Debug.LogWarning($"[Server] Unknown packet type: {packetType}");
        }
    }

    void OnDestroy()
    {
        if (driver.IsCreated)
        {
            // Disconnect all clients cleanly before shutting down
            for (int i = 0; i < connections.Length; i++)
            {
                if (connections[i].IsCreated)
                    driver.Disconnect(connections[i]);
            }

            driver.Dispose();
            connections.Dispose();
        }
    }
}
