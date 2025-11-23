using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class StyleController : MonoBehaviour
{
    [Header("Style Controller Settings")]
    public GameObject StyleMeterPanel;
    public static StyleController main;
    public DoubleTxt StylemeterTitleTxt;
    public DoubleTxt StylemeterDescTxt;
    public DoubleTxt ScoreMult;
    public Slider CountdownTimer;
    public Slider CountdownScore;

    public StyleRank[] StyleRanks;

    [Header("Scoring Settings")]
    private float currentPoints = 0f;
    public float decayRate = 5f;         // poin berkurang per detik
    public float comboTimeout = 15f;     // waktu sebelum reset total
    private float lastActionTime;
    private int currentRankIndex = -1;

    [Header("UI Transition")]
    public Image TopCorner;
    public Image BottomCorner;
    public float transitionSpeed = 1f;
    [Range(0,1)]public float transitionValue = 0f;

    private void Awake()
    {
        main = this;
    }

    private void Start()
    {
        setActive(false);
    }

    private void Update()
    {
        if (!isActive()) return;

        float timeSinceLastAction = Time.time - lastActionTime;

        if (timeSinceLastAction < comboTimeout)
        {
            currentPoints -= decayRate * Time.deltaTime;
            currentPoints = Mathf.Max(0, currentPoints);
            UpdateRankUI();
        }
        else
        {
            ResetMeter();
        }

        if (currentPoints <= 0)
        {
            setActive(false);
        }
    }

    public void AddPoints(float value)
    {
        currentPoints += value;

        lastActionTime = Time.time;

        UpdateRankUI();
    }

    private void UpdateRankUI()
    {
        int newRankIndex = -1;

        for (int i = 0; i < StyleRanks.Length; i++)
        {
            if (currentPoints >= StyleRanks[i].PointsNeeded)
            {
                newRankIndex = i;
            }
            else
            {
                break;
            }
        }

        // jika rank berubah, update tampilannya
        if (newRankIndex != currentRankIndex &&newRankIndex>= 0)
        {
            currentRankIndex = newRankIndex;
            OnRankChanged(StyleRanks[currentRankIndex]);
        }

        if (CountdownTimer)
            CountdownTimer.value = Mathf.Clamp01(1-((Time.time - lastActionTime) / comboTimeout));

        if (CountdownScore)
        {
            int nextRankIndex = Mathf.Min(currentRankIndex + 1, StyleRanks.Length - 1);
            float needed = StyleRanks[nextRankIndex].PointsNeeded;
            CountdownScore.value = Mathf.Clamp01(currentPoints / needed);
        }
    }

    void setActive(bool value)
    {
        if (StyleMeterPanel)
        {
            if (value && !isActive())
            {
                StartCoroutine(fadeInTransition(transitionSpeed));
            }
            else if (!value && isActive())
            {
                StartCoroutine(fadeOutTransition(transitionSpeed));
            }
        }
    }
        
    private IEnumerator fadeInTransition(float speed)
    {
        StyleMeterPanel.SetActive(true);
        while (transitionValue < 1f)
        {
            transitionValue += Time.deltaTime * speed;
            TopCorner.fillAmount = transitionValue;
            BottomCorner.fillAmount = transitionValue;
            yield return null;
        }
        transitionValue = 1f;
        TopCorner.fillAmount = 1f;
        BottomCorner.fillAmount = 1f;

    }

    private IEnumerator fadeOutTransition(float speed)
    {
        while (transitionValue > 0f)
        {
            transitionValue -= Time.deltaTime * speed;
            TopCorner.fillAmount = transitionValue;
            BottomCorner.fillAmount = transitionValue;
            yield return null;
        }
        transitionValue = 0f;
        TopCorner.fillAmount = 0f;
        BottomCorner.fillAmount = 0f;

        StyleMeterPanel.SetActive(false);
    }

    bool isActive()
    {
        return StyleMeterPanel && StyleMeterPanel.activeSelf;
    }

    public float GetCurrentMultiplier()
    {
        return StyleRanks[currentRankIndex].Multiplier;
    }

    public void ResetMeter()
    {
        setActive(false);
        currentPoints = 0;
        currentRankIndex = -1;
    }

    private void OnRankChanged(StyleRank newRank)
    {
        if (!isActive()) setActive(true);

        StylemeterTitleTxt.SetText(newRank.BigStyleName);
        StylemeterDescTxt.SetText(newRank.SmallStyleName);
        ScoreMult.SetText($"x{newRank.Multiplier:F1}");
        StylemeterTitleTxt.SetBackColor(newRank.Color);
    }
    public int getCurrentPointsNeeded()
    {
        return StyleRanks[currentRankIndex++].PointsNeeded;
    }
}

[System.Serializable]
public class StyleRank
{
    public string BigStyleName;
    public string SmallStyleName;
    public float Multiplier;
    public int PointsNeeded;
    public Color Color;
}
