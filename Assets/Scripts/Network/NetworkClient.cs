using UnityEngine;
using UnityEngine.Tilemaps;
using Unity.Networking.Transport;

/// <summary>
/// Client-side network component.
/// Connects to the server, sends local player packets via SendPosition()
/// and SendMine(), and applies received data to the remote player objects.
/// Attach this to the same GameObject as LocalPlayer (or a NetworkManager object).
/// </summary>
public class NetworkClient : MonoBehaviour
{
    [Header("Server Address")]
    public string serverIP   = "127.0.0.1";
    public ushort serverPort = 9000;

    [Header("Scene References")]
    [Tooltip("The opponent's GameObject whose position we will update.")]
    public GameObject remotePlayer;

    [Tooltip("The shared tilemap — used to remove tiles mined by the remote player.")]
    public Tilemap tilemap;

    // Internal transport state
    private NetworkDriver     driver;
    private NetworkConnection connection;

    // Track connection state so we only log once per state change
    private bool isConnected = false;

    void Start()
    {
        driver     = NetworkDriver.Create();
        connection = default;

        // Parse the server endpoint and initiate connection
        if (!NetworkEndpoint.TryParse(serverIP, serverPort, out var endpoint))
        {
            Debug.LogError($"[Client] Could not parse endpoint {serverIP}:{serverPort}");
            return;
        }

        connection = driver.Connect(endpoint);
        Debug.Log($"[Client] Connecting to {serverIP}:{serverPort}...");
    }

    void Update()
    {
        // Flush outgoing and process incoming messages
        driver.ScheduleUpdate().Complete();

        if (!connection.IsCreated) return;

        // Process all pending events on our single connection
        NetworkEvent.Type evt;
        while ((evt = driver.PopEventForConnection(connection, out var stream)) != NetworkEvent.Type.Empty)
        {
            switch (evt)
            {
                case NetworkEvent.Type.Connect:
                    isConnected = true;
                    Debug.Log("[Client] Connected to server.");
                    break;

                case NetworkEvent.Type.Data:
                    HandleIncomingData(ref stream);
                    break;

                case NetworkEvent.Type.Disconnect:
                    isConnected = false;
                    Debug.Log("[Client] Disconnected from server.");
                    connection = default;
                    break;
            }
        }
    }

    // Route incoming packets to the correct handler based on type byte
    private void HandleIncomingData(ref Unity.Collections.DataStreamReader stream)
    {
        byte packetType = stream.ReadByte();

        if (packetType == PacketType.Position)
        {
            float x = stream.ReadFloat();
            float y = stream.ReadFloat();

            // Move the remote player sprite to the position reported by the server
            if (remotePlayer != null)
                remotePlayer.transform.position = new Vector3(x, y, remotePlayer.transform.position.z);
        }
        else if (packetType == PacketType.Mine)
        {
            int cellX = stream.ReadInt();
            int cellY = stream.ReadInt();

            // Remove the tile that the remote player mined on our local tilemap
            if (tilemap != null)
                tilemap.SetTile(new Vector3Int(cellX, cellY, 0), null);
        }
        else
        {
            Debug.LogWarning($"[Client] Unknown packet type received: {packetType}");
        }
    }

    // -----------------------------------------------------------------
    //  Public API called by LocalPlayer every network tick
    // -----------------------------------------------------------------

    /// <summary>Sends our local player's world position to the server.</summary>
    public void SendPosition(float x, float y)
    {
        if (!isConnected || !connection.IsCreated) return;

        driver.BeginSend(connection, out var writer);
        writer.WriteByte(PacketType.Position);
        writer.WriteFloat(x);
        writer.WriteFloat(y);
        driver.EndSend(writer);
    }

    /// <summary>
    /// Sends a mine event (the cell coordinates of the destroyed tile)
    /// so the server can relay it to the opponent.
    /// </summary>
    public void SendMine(int cellX, int cellY)
    {
        if (!isConnected || !connection.IsCreated) return;

        driver.BeginSend(connection, out var writer);
        writer.WriteByte(PacketType.Mine);
        writer.WriteInt(cellX);
        writer.WriteInt(cellY);
        driver.EndSend(writer);
    }

    void OnDestroy()
    {
        if (driver.IsCreated)
        {
            if (connection.IsCreated)
                driver.Disconnect(connection);

            driver.Dispose();
        }
    }
}
