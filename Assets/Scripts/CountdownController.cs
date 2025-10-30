using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; 

public class CountdownTransition : MonoBehaviour
{
    public Sprite[] countdownSprites; 

    public Image displayImage; 
    
    public float durationImage = 1f;

    private int indexImage = 0; 
    private float timer;

    void Start()
    {
        displayImage.sprite = countdownSprites[0];
        timer = durationImage; 
    }

    void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= 0)
        {
            indexImage++; 

            if (indexImage < countdownSprites.Length)
            {
                displayImage.sprite = countdownSprites[indexImage];
                timer = durationImage; 
            }
            else
            {
                SceneManager.LoadScene("GameScene");
                enabled = false; 
            }
        }
    }
}