using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public DoubleTxt scoreTxt;
    [SerializeField] private int score = 0;
    public static ScoreManager instance;

    // Expose the current score as a read-only property for other scripts
    public int Score => score;

    void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        updateTxt(scoreTxt, score.ToString());
    }

    public void AddScore(int amount)
    {
        score += amount;
        StyleController.main.AddPoints(amount);
        updateTxt(scoreTxt, score.ToString());
    }

    private void updateTxt(DoubleTxt dt, string text)
    {
        if (dt != null) dt.SetText(text);
    }

    // Optional: public setter if you ever want to set score directly
    public void SetScore(int newScore)
    {
        score = newScore;
        updateTxt(scoreTxt, score.ToString());
    }
}
