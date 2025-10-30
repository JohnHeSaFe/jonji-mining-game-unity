using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement; 

public class EndGameController : MonoBehaviour
{
    public TextMeshProUGUI winnerText;
    public TextMeshProUGUI blueTotalText;
    public TextMeshProUGUI redTotalText;

    [Header("Blue")]
    public TextMeshProUGUI blueDirtText;
    public TextMeshProUGUI blueCoalText;
    public TextMeshProUGUI blueGoldText;
    public TextMeshProUGUI blueDiamondText;
    public TextMeshProUGUI blueEmeraldText;
    public TextMeshProUGUI blueRubyText;

    [Header("Red")]
    public TextMeshProUGUI redDirtText;
    public TextMeshProUGUI redCoalText;
    public TextMeshProUGUI redGoldText;
    public TextMeshProUGUI redDiamondText;
    public TextMeshProUGUI redEmeraldText;
    public TextMeshProUGUI redRubyText;

    void Start()
    {
        ShowResults();
    }

    void ShowResults()
    {
        if (GameManager.game.scorePlayer1 > GameManager.game.scorePlayer2)
        {
            winnerText.color = Color.blue;
            winnerText.text = "BLUE PLAYER WINS!";
        }
        else if (GameManager.game.scorePlayer2 > GameManager.game.scorePlayer1)
        {
            winnerText.color = Color.red;
            winnerText.text = "RED PLAYER WINS!";
        }
        else
        {
            winnerText.text = "IT'S A TIE!";
        }
            
        blueTotalText.text = "Blue: " + GameManager.game.scorePlayer1;
        redTotalText.text = "Red: " + GameManager.game.scorePlayer2;

        foreach (MineralData mineral in GameManager.game.listMinerals)
        {
            int blueAmount = GameManager.game.collectedTilesPlayer1[mineral.tile];
            int redAmount = GameManager.game.collectedTilesPlayer2[mineral.tile];
            
            switch (mineral.name)
            {
                case "dirt":
                    if (blueDirtText != null) blueDirtText.text = "x " + blueAmount;
                    if (redDirtText != null) redDirtText.text = "x " + redAmount;
                    break;
                case "coal":
                    if (blueCoalText != null) blueCoalText.text = "x " + blueAmount;
                    if (redCoalText != null) redCoalText.text = "x " + redAmount;
                    break;
                case "gold":
                    if (blueGoldText != null) blueGoldText.text = "x " + blueAmount;
                    if (redGoldText != null) redGoldText.text = "x " + redAmount;
                    break;
                case "diamond":
                    if (blueDiamondText != null) blueDiamondText.text = "x " + blueAmount;
                    if (redDiamondText != null) redDiamondText.text = "x " + redAmount;
                    break;
                case "emerald":
                    if (blueEmeraldText != null) blueEmeraldText.text = "x " + blueAmount;
                    if (redEmeraldText != null) redEmeraldText.text = "x " + redAmount;
                    break;
                case "ruby":
                    if (blueRubyText != null) blueRubyText.text = "x " + blueAmount;
                    if (redRubyText != null) redRubyText.text = "x " + redAmount;
                    break;
            }
        }
    }

    public void BackToMenu()
    {
        if (GameManager.game != null)
        {
            Destroy(GameManager.game.gameObject);
        }
        
        SceneManager.LoadScene(0);
    }
}