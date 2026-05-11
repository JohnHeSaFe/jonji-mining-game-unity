

public static class NetworkMessages
{
    public enum MsgType : byte
    {
        ClientReady   = 0,
        StartGame     = 1,
        PlayerMove    = 2,
        TileMined     = 3,
        ScoreSync     = 4,
        GameStateSync = 5,
    }

    // --- Write helpers ---

    public static void WriteClientReady(ref Unity.Collections.DataStreamWriter w)
    {
        w.WriteByte((byte)MsgType.ClientReady);
    }

    public static void WriteStartGame(ref Unity.Collections.DataStreamWriter w, int seed)
    {
        w.WriteByte((byte)MsgType.StartGame);
        w.WriteInt(seed);
    }

    public static void WritePlayerMove(ref Unity.Collections.DataStreamWriter w, byte playerID,
        float posX, float posY, float velX, float velY)
    {
        w.WriteByte((byte)MsgType.PlayerMove);
        w.WriteByte(playerID);
        w.WriteFloat(posX);
        w.WriteFloat(posY);
        w.WriteFloat(velX);
        w.WriteFloat(velY);
    }

    public static void WriteTileMined(ref Unity.Collections.DataStreamWriter w, byte playerID, int tileX, int tileY)
    {
        w.WriteByte((byte)MsgType.TileMined);
        w.WriteByte(playerID);
        w.WriteInt(tileX);
        w.WriteInt(tileY);
    }

    public static void WriteScoreSync(ref Unity.Collections.DataStreamWriter w, int score1, int score2)
    {
        w.WriteByte((byte)MsgType.ScoreSync);
        w.WriteInt(score1);
        w.WriteInt(score2);
    }

    public static void WriteGameStateSync(ref Unity.Collections.DataStreamWriter w, float timeRemaining, byte state)
    {
        w.WriteByte((byte)MsgType.GameStateSync);
        w.WriteFloat(timeRemaining);
        w.WriteByte(state);
    }
}
