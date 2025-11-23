using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public DoubleTxt scoreTxt;
    public DoubleTxt highScoreTxt;
    [SerializeField]private int score = 0;
    public static ScoreManager instance;

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
    private void updateTxt(DoubleTxt dt,string text)
    {
        dt.SetText(text);
    }
}
