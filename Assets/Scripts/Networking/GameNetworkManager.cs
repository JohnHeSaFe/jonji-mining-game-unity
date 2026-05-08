using System;
using System.Collections.Generic;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameNetworkManager : MonoBehaviour
{
    public static GameNetworkManager Instance { get; private set; }

    public enum Role { None, Server, Client }
    public Role CurrentRole { get; private set; } = Role.None;

    public int LocalPlayerID { get; private set; } = -1;
    public bool GameStarted { get; private set; } = false;

    public event Action OnGameStart;
    public event Action<byte, float, float, float, float> OnRemotePlayerMove;
    public event Action<byte, int, int> OnTileMinedRemote;
    public event Action<int, int> OnScoreSync;
    public event Action<float, byte> OnGameStateSync;

    private NetworkDriver _driver;
    private List<NetworkConnection> _serverConnections = new List<NetworkConnection>();
    private NetworkConnection _clientConnection;
    private bool _isDriverCreated = false;

    private float _stateSyncTimer = 0f;
    private const float StateSyncInterval = 0.5f;
    private const ushort Port = 7777;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Application.runInBackground = true;
    }

    void OnDestroy()
    {
        Shutdown();
    }

    // ── Public API ──────────────────────────────────────────────────────────

    public bool StartServer()
    {
        _driver = NetworkDriver.Create();
        var endpoint = NetworkEndpoint.AnyIpv4.WithPort(Port);
        if (_driver.Bind(endpoint) != 0 || _driver.Listen() != 0)
        {
            Debug.LogError("[Net] Failed to bind/listen on port " + Port);
            _driver.Dispose();
            return false;
        }
        CurrentRole = Role.Server;
        LocalPlayerID = 1;
        _isDriverCreated = true;
        Debug.Log("[Net] Server listening on port " + Port);
        return true;
    }

    public bool StartClient(string ip)
    {
        _driver = NetworkDriver.Create();
        if (!NetworkEndpoint.TryParse(ip, Port, out var endpoint))
        {
            Debug.LogError("[Net] Invalid IP: " + ip);
            _driver.Dispose();
            return false;
        }
        _clientConnection = _driver.Connect(endpoint);
        CurrentRole = Role.Client;
        LocalPlayerID = 2;
        _isDriverCreated = true;
        Debug.Log("[Net] Client connecting to " + ip + ":" + Port);
        return true;
    }

    public void Shutdown()
    {
        if (!_isDriverCreated) return;
        foreach (var c in _serverConnections) c.Disconnect(_driver);
        _serverConnections.Clear();
        if (_clientConnection.IsCreated) _clientConnection.Disconnect(_driver);
        _driver.Dispose();
        _isDriverCreated = false;
        CurrentRole = Role.None;
        GameStarted = false;
    }

    // ── Send helpers ────────────────────────────────────────────────────────

    public void SendPlayerMove(byte playerID, float px, float py, float vx, float vy)
    {
        if (!_isDriverCreated) return;
        if (CurrentRole == Role.Client)
        {
            _driver.BeginSend(_clientConnection, out var w);
            NetworkMessages.WritePlayerMove(ref w, playerID, px, py, vx, vy);
            _driver.EndSend(w);
        }
        else if (CurrentRole == Role.Server)
        {
            BroadcastAll((ref Unity.Collections.DataStreamWriter w) =>
                NetworkMessages.WritePlayerMove(ref w, playerID, px, py, vx, vy));
        }
    }

    public void SendTileMined(byte playerID, int tx, int ty)
    {
        if (!_isDriverCreated) return;
        if (CurrentRole == Role.Client)
        {
            _driver.BeginSend(_clientConnection, out var w);
            NetworkMessages.WriteTileMined(ref w, playerID, tx, ty);
            _driver.EndSend(w);
        }
        else if (CurrentRole == Role.Server)
        {
            BroadcastAll((ref Unity.Collections.DataStreamWriter w) =>
                NetworkMessages.WriteTileMined(ref w, playerID, tx, ty));
        }
    }

    // ── Unity Update ────────────────────────────────────────────────────────

    void Update()
    {
        if (!_isDriverCreated) return;
        _driver.ScheduleUpdate().Complete();

        if (CurrentRole == Role.Server) ServerUpdate();
        else if (CurrentRole == Role.Client) ClientUpdate();

        if (CurrentRole == Role.Server && GameStarted)
        {
            _stateSyncTimer += Time.deltaTime;
            if (_stateSyncTimer >= StateSyncInterval)
            {
                _stateSyncTimer = 0f;
                ServerSendGameState();
            }
        }
    }

    // ── Server ──────────────────────────────────────────────────────────────

    void ServerUpdate()
    {
        NetworkConnection c;
        while ((c = _driver.Accept()) != default)
        {
            _serverConnections.Add(c);
            Debug.Log("[Net] Client connected. Total: " + _serverConnections.Count);
            if (_serverConnections.Count == 1)
                SendStartGameToAll();
        }

        foreach (var conn in _serverConnections)
        {
            if (!conn.IsCreated) continue;
            NetworkEvent.Type cmd;
            while ((cmd = _driver.PopEventForConnection(conn, out var stream)) != NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Data)
                    ServerHandleData(conn, ref stream);
                else if (cmd == NetworkEvent.Type.Disconnect)
                    Debug.Log("[Net] A client disconnected.");
            }
        }
    }

    void ServerHandleData(NetworkConnection sender, ref Unity.Collections.DataStreamReader stream)
    {
        var msgType = (NetworkMessages.MsgType)stream.ReadByte();
        switch (msgType)
        {
            case NetworkMessages.MsgType.ClientReady:
                break;

            case NetworkMessages.MsgType.PlayerMove:
            {
                byte pid = stream.ReadByte();
                float px = stream.ReadFloat(), py = stream.ReadFloat();
                float vx = stream.ReadFloat(), vy = stream.ReadFloat();
                BroadcastExcept(sender, (ref Unity.Collections.DataStreamWriter w) =>
                    NetworkMessages.WritePlayerMove(ref w, pid, px, py, vx, vy));
                // Also apply locally on server so server sees client 2 moving
                OnRemotePlayerMove?.Invoke(pid, px, py, vx, vy);
                break;
            }
            case NetworkMessages.MsgType.TileMined:
            {
                byte pid = stream.ReadByte();
                int tx = stream.ReadInt(), ty = stream.ReadInt();
                BroadcastExcept(sender, (ref Unity.Collections.DataStreamWriter w) =>
                    NetworkMessages.WriteTileMined(ref w, pid, tx, ty));
                OnTileMinedRemote?.Invoke(pid, tx, ty);
                break;
            }
        }
    }

    void SendStartGameToAll()
    {
        GameStarted = true;
        foreach (var conn in _serverConnections)
        {
            _driver.BeginSend(conn, out var w);
            NetworkMessages.WriteStartGame(ref w);
            _driver.EndSend(w);
        }
        OnGameStart?.Invoke();
        SceneManager.LoadScene("CountdownScene");
    }

    void ServerSendGameState()
    {
        if (GameManager.game == null) return;
        float t = GameManager.game.GetCurrentGameTime();
        byte state = (byte)GameManager.game.currentState;
        int s1 = GameManager.game.scorePlayer1;
        int s2 = GameManager.game.scorePlayer2;

        BroadcastAll((ref Unity.Collections.DataStreamWriter w) =>
            NetworkMessages.WriteGameStateSync(ref w, t, state));
        BroadcastAll((ref Unity.Collections.DataStreamWriter w) =>
            NetworkMessages.WriteScoreSync(ref w, s1, s2));
    }

    // ── Client ──────────────────────────────────────────────────────────────

    void ClientUpdate()
    {
        NetworkEvent.Type cmd;
        while ((cmd = _driver.PopEventForConnection(_clientConnection, out var stream)) != NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Connect)
            {
                Debug.Log("[Net] Connected to server.");
                _driver.BeginSend(_clientConnection, out var w);
                NetworkMessages.WriteClientReady(ref w);
                _driver.EndSend(w);
            }
            else if (cmd == NetworkEvent.Type.Data)
            {
                ClientHandleData(ref stream);
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                Debug.Log("[Net] Disconnected from server.");
                _clientConnection = default;
            }
        }
    }

    void ClientHandleData(ref Unity.Collections.DataStreamReader stream)
    {
        var msgType = (NetworkMessages.MsgType)stream.ReadByte();
        switch (msgType)
        {
            case NetworkMessages.MsgType.StartGame:
                GameStarted = true;
                OnGameStart?.Invoke();
                SceneManager.LoadScene("CountdownScene");
                break;

            case NetworkMessages.MsgType.PlayerMove:
            {
                byte pid = stream.ReadByte();
                float px = stream.ReadFloat(), py = stream.ReadFloat();
                float vx = stream.ReadFloat(), vy = stream.ReadFloat();
                OnRemotePlayerMove?.Invoke(pid, px, py, vx, vy);
                break;
            }
            case NetworkMessages.MsgType.TileMined:
            {
                byte pid = stream.ReadByte();
                int tx = stream.ReadInt(), ty = stream.ReadInt();
                OnTileMinedRemote?.Invoke(pid, tx, ty);
                break;
            }
            case NetworkMessages.MsgType.ScoreSync:
            {
                int s1 = stream.ReadInt(), s2 = stream.ReadInt();
                if (GameManager.game != null)
                {
                    GameManager.game.scorePlayer1 = s1;
                    GameManager.game.scorePlayer2 = s2;
                }
                break;
            }
            case NetworkMessages.MsgType.GameStateSync:
            {
                float t = stream.ReadFloat();
                byte st = stream.ReadByte();
                if (GameManager.game != null)
                    GameManager.game.SetNetworkTime(t, (GameManager.GameState)st);
                break;
            }
        }
    }

    // ── Broadcast helpers ────────────────────────────────────────────────────

    delegate void WriteDelegate(ref Unity.Collections.DataStreamWriter w);

    void BroadcastAll(WriteDelegate write)
    {
        foreach (var conn in _serverConnections)
        {
            _driver.BeginSend(conn, out var w);
            write(ref w);
            _driver.EndSend(w);
        }
    }

    void BroadcastExcept(NetworkConnection except, WriteDelegate write)
    {
        foreach (var conn in _serverConnections)
        {
            if (conn == except) continue;
            _driver.BeginSend(conn, out var w);
            write(ref w);
            _driver.EndSend(w);
        }
    }
}
