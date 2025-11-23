using System.Collections;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UiGameScript : MonoBehaviour
{
    public GameObject car;
    public GameObject diaper;
    public float waitTime = 2f;
    public Animator Animator;
    public Material Material;
    public TMP_Text highScoreText;   // UI element to show the saved high score (e.g. "High Score: 123")
    public TMP_Text finalScoreText;  // UI element to show the final score for this run
    public ScoreManager scoreManager; // your existing score manager; must expose an int Score or similar

    private void Start()
    {
        // load and display current best score
        if (highScoreText != null)
        {
            int best = HighScoreManager.Instance.GetBestScore();
            highScoreText.text = "High Score: " + best;
        }
    }

    public void Startgame()
    {
        StartCoroutine(setGame());
    }

    private IEnumerator setGame()
    {
        AudioManager.Main.PlaySound("blh", 0.5f, 0.9f, true);
        if (Animator != null) Animator.Play("Start");
        if (car != null) car.SetActive(true);
        yield return new WaitForSeconds(waitTime);
        if (diaper != null) diaper.SetActive(true);
    }

    /// <summary>
    /// Call this when the run ends. It updates the UI and saves a new best score if appropriate.
    /// </summary>
    public void StopGame()
    {
        AudioManager.Main.PauseOrStop("blh");

        int finalScore = 0;

        if (scoreManager != null)
        {
            // try to read an integer score. Adjust to your ScoreManager API.
            // I assume scoreManager has an int field/property called 'scoreTxt' or 'score'.
            // If it is a string (like "123"), try parsing it.
            // Example adaptions below:
#if UNITY_EDITOR
            // Debug help
#endif

            // Prefer integer property if available:
            var scoreProp = scoreManager.GetType().GetProperty("score");
            if (scoreProp != null)
            {
                object val = scoreProp.GetValue(scoreManager);
                if (val is int i) finalScore = i;
                else if (val is float f) finalScore = Mathf.RoundToInt(f);
            }
            else
            {
                // fallback: check for public int field named 'score'
                var field = scoreManager.GetType().GetField("score");
                if (field != null)
                {
                    object val = field.GetValue(scoreManager);
                    if (val is int i) finalScore = i;
                    else if (val is float f) finalScore = Mathf.RoundToInt(f);
                }
                else
                {
                    // ultimate fallback: string 'scoreTxt' (your original usage)
                    try
                    {
                        var propTxt = scoreManager.GetType().GetProperty("scoreTxt");
                        if (propTxt != null)
                        {
                            object val = propTxt.GetValue(scoreManager);
                            if (val != null)
                                int.TryParse(val.ToString(), out finalScore);
                        }
                        else
                        {
                            var fieldTxt = scoreManager.GetType().GetField("scoreTxt");
                            if (fieldTxt != null)
                            {
                                object val = fieldTxt.GetValue(scoreManager);
                                if (val != null)
                                    int.TryParse(val.ToString(), out finalScore);
                            }
                        }
                    }
                    catch { /* ignore reflection errors */ }
                }
            }
        }

        // update final score UI
        if (finalScoreText != null)
            finalScoreText.text = "Final Score: " + finalScore;

        // try to save to highscore manager
        if (HighScoreManager.Instance.TrySetBestScore(finalScore))
        {
            // new highscore!
            if (highScoreText != null)
                highScoreText.text = "High Score: " + HighScoreManager.Instance.GetBestScore();

            // Optional: play a "new record" animation or sound here
        }
        else
        {
            // no new highscore, still display best
            if (highScoreText != null)
                highScoreText.text = "High Score: " + HighScoreManager.Instance.GetBestScore();
        }

        if (Animator != null) Animator.Play("End");
    }

    public void Restart()
    {
        Time.timeScale = 1f;

        Scene active = SceneManager.GetActiveScene();
        SceneManager.LoadScene(active.buildIndex);
    }

    // Optional helper for a UI button to reset high score (for debugging)
    public void ResetHighScore()
    {
        HighScoreManager.Instance.ResetHighScore();
        if (highScoreText != null)
            highScoreText.text = "High Score: " + HighScoreManager.Instance.GetBestScore();
    }
}
