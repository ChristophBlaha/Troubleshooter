using UnityEngine;
using System;
using TMPro;

public class Score : MonoBehaviour
{
    private int score;
    public TextMeshProUGUI scoreText;
    public static Score Instance { get; private set; }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Update()
    {
        if(scoreText!=null)
            scoreText.text = score.ToString();
    }

    public void IncreaseScore(int amount)
    {
        score += amount;
    }
    
    public int GetScore()
    {
        return score;
    }
}
