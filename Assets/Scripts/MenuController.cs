using UnityEngine;
using UnityEngine.SceneManagement; 

public class MenuController : MonoBehaviour
{
    public GameObject guidePanel;

    public void StartGame()
    {
        SceneManager.LoadScene("CountdownScene");
    }

    public void ShowGuide()
    {
        if (guidePanel != null)
        {
            guidePanel.SetActive(true); 
        }
    }

    public void HideGuide()
    {
        if (guidePanel != null)
        {
            guidePanel.SetActive(false); 
        }
    }
}