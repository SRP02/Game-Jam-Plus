using System.Collections;
using TMPro;
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
    public ScoreManager scoreManager; // assign in inspector (should reference the ScoreManager in scene)

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
        // play start sound (ensure AudioManager.Main exists)
        if (AudioManager.Main != null) AudioManager.Main.PlaySound("blh", 0.5f, 0.9f, true);

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
        if (AudioManager.Main != null) AudioManager.Main.PauseOrStop("blh");

        int finalScore = 0;

        if (scoreManager != null)
        {
            finalScore = scoreManager.Score;
        }
        else
        {
            Debug.LogWarning("UiGameScript.StopGame: scoreManager not assigned in inspector.");
        }

        // update final score UI
        if (finalScoreText != null)
            finalScoreText.text = "Final Score: " + finalScore;

        // try to save to highscore manager
        bool isNewRecord = HighScoreManager.Instance.TrySetBestScore(finalScore);

        // display updated best
        if (highScoreText != null)
            highScoreText.text = "High Score: " + HighScoreManager.Instance.GetBestScore();

        if (isNewRecord)
        {
            // optional: play new-record feedback
            Debug.Log("New High Score! " + finalScore);
            // e.g. Animator.Play("NewRecord"); or play a sound
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
