using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic; 
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager game;

    public int scorePlayer1 = 0;
    public int scorePlayer2 = 0;

    public Dictionary<TileBase, int> collectedTilesPlayer1 = new Dictionary<TileBase, int>();
    public Dictionary<TileBase, int> collectedTilesPlayer2 = new Dictionary<TileBase, int>();

    public List<MineralData> listMinerals;

    public enum GameState { Playing, EndGame } 
    public GameState currentState;
    public float gameDuration = 60f; 
    private float currentGameTimer;

    void Start()
    {   
        currentGameTimer = gameDuration;
        currentState = GameState.Playing;
        if (game == null)
        {
            game = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        
        foreach (MineralData mineral in listMinerals)
        {
            if (!collectedTilesPlayer1.ContainsKey(mineral.tile))
                collectedTilesPlayer1.Add(mineral.tile, 0);
            
            if (!collectedTilesPlayer2.ContainsKey(mineral.tile))
                collectedTilesPlayer2.Add(mineral.tile, 0);
        }
    }

    void Update()
    {
        if (currentState == GameState.Playing)
        {
            currentGameTimer -= Time.deltaTime;

            if (currentGameTimer <= 0)
            {
                currentGameTimer = 0;
                currentState = GameState.EndGame;

                SceneManager.LoadScene("EndGameScene");
            }
        }
    }

    public void AddPoints(int playerID, TileBase minedMineral)
    {
        foreach (MineralData mineral in listMinerals)
        {
            if (mineral.tile == minedMineral)
            {
                if (playerID == 1)
                {
                    scorePlayer1 += mineral.points;

                    if (collectedTilesPlayer1.ContainsKey(minedMineral))
                    {
                        collectedTilesPlayer1[minedMineral]++;
                    }
                }
                else if (playerID == 2)
                {
                    scorePlayer2 += mineral.points;

                    if (collectedTilesPlayer2.ContainsKey(minedMineral))
                    {
                        collectedTilesPlayer2[minedMineral]++;
                    }
                }
                return;
            }
        }
    }
    
    public float GetCurrentGameTime()
    {
        return currentGameTimer;
    }
}

[System.Serializable]
public class MineralData
{
    public string name; 
    public TileBase tile;
    public int points;
}