using UnityEngine;
using TMPro; 

public class UIManager : MonoBehaviour
{
    public TextMeshProUGUI blueText;
    public TextMeshProUGUI redText;

    public TextMeshProUGUI textoTimeRemaining;

    void Update()
    {
        blueText.text = "Blue: " + GameManager.game.scorePlayer1;
        redText.text = "Red: " + GameManager.game.scorePlayer2;

        if (GameManager.game.currentState == GameManager.GameState.Playing)
        {
            float time = GameManager.game.GetCurrentGameTime();
            int segundosTotales = Mathf.CeilToInt(time);
            textoTimeRemaining.text = segundosTotales.ToString() + " s";

            if (segundosTotales <= 10)
            {
                textoTimeRemaining.color = Color.red;
            }
            else
            {
                textoTimeRemaining.color = Color.white;
            }
        }
        else if (GameManager.game.currentState == GameManager.GameState.EndGame)
        {
            textoTimeRemaining.text = "0 s";
        }
    }
}