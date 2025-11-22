using TMPro;
using UnityEngine;

public class DoubleTxt : MonoBehaviour
{
    [HideInInspector] public TextMeshProUGUI _textEffect;
    [HideInInspector] public TextMeshProUGUI _textContent;

    private void Awake()
    {
        ReferenceTexts();
    }

    void ReferenceTexts()
    {
        _textEffect = GetComponent<TextMeshProUGUI>();
        _textContent = transform.GetChild(0).GetComponent<TextMeshProUGUI>();
    }

    public void SetText(string message)
    {
        if (_textEffect == null || _textContent == null)
        {
            ReferenceTexts();
        }

        _textEffect.text = message;
        _textContent.text = message;
    }

    public void SetSize(float value)
    {
        if (_textEffect == null || _textContent == null)
        {
            ReferenceTexts();
        }

        _textEffect.fontSize = value;
        _textContent.fontSize = value;
    }

    public void SetMinSize(float value)
    {
        if (_textEffect == null || _textContent == null)
        {
            ReferenceTexts();
        }

        _textEffect.fontSizeMin = value;
        _textContent.fontSizeMin = value;
    }

    public void SetMaxSize(float value)
    {
        if (_textEffect == null || _textContent == null)
        {
            ReferenceTexts();
        }

        _textEffect.fontSizeMax = value;
        _textContent.fontSizeMax = value;
    }

    public string GetText()
    {
        return _textContent.text;
    }

    public void SetColor(Color color)
    {
        if (_textEffect == null || _textContent == null)
        {
            ReferenceTexts();
        }

        _textContent.color = color;
    }
}
