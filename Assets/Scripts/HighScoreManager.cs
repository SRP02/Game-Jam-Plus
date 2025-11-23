using System.IO;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class HighScoreManager : MonoBehaviour
{
    private static HighScoreManager _instance;
    public static HighScoreManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("HighScoreManager");
                _instance = go.AddComponent<HighScoreManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private string savePath => Path.Combine(Application.persistentDataPath, "highscore.json");

    [System.Serializable]
    private class SaveData
    {
        public int bestScore;
    }

    private int bestScore = 0;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(this.gameObject);
        Load();
    }

    public int GetBestScore() => bestScore;

    public bool TrySetBestScore(int score)
    {
        if (score > bestScore)
        {
            bestScore = score;
            Save();
            return true;
        }
        return false;
    }

    private void Save()
    {
        try
        {
            SaveData data = new SaveData { bestScore = bestScore };
            string json = JsonUtility.ToJson(data);
            File.WriteAllText(savePath, json);
#if UNITY_EDITOR
            Debug.Log($"HighScoreManager: saved bestScore={bestScore} to {savePath}");
#endif
        }
        catch (System.Exception e)
        {
            Debug.LogError("HighScoreManager: Failed to save high score: " + e);
        }
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(savePath))
            {
                bestScore = 0;
                return;
            }

            string json = File.ReadAllText(savePath);
            var data = JsonUtility.FromJson<SaveData>(json);
            bestScore = data != null ? data.bestScore : 0;
#if UNITY_EDITOR
            Debug.Log($"HighScoreManager: loaded bestScore={bestScore} from {savePath}");
#endif
        }
        catch (System.Exception e)
        {
            Debug.LogError("HighScoreManager: Failed to load high score: " + e);
            bestScore = 0;
        }
    }

    public void ResetHighScore()
    {
        bestScore = 0;
        try
        {
            if (File.Exists(savePath))
                File.Delete(savePath);
        }
        catch (System.Exception e)
        {
            Debug.LogError("HighScoreManager: Failed to delete save: " + e);
        }
    }
}
