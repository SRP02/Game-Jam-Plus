using UnityEngine;

public class TransformShake : MonoBehaviour
{
    [Header("Config")]
    [SerializeField, Range(0f, 360f)] public float m_angle = 0f;
    [SerializeField] private float m_strength = 1f;
    [SerializeField] private float m_frequency = 25f;
    [SerializeField] private float m_duration = 0.5f;
    [Space, SerializeField] private bool m_waitingUntilFinished = false;

    #region INTERNAL
    private Vector3 _originalLocalPos;   // Store local position for non-intrusive shake
    private float _currTime = 0f;
    private bool _inShake = false;
    private float _currShakeMultiplier = 1f;
    private Vector2 _shakeNormal;
    #endregion

    /// <summary>
    /// Shake with default inspector parameters
    /// </summary>
    public void Shake() => Shake(m_angle, m_strength, m_frequency, m_duration);

    /// <summary>
    /// Shake with custom parameters
    /// </summary>
    public void Shake(float angle, float strength = 1f, float frequency = 50f, float duration = 0.5f)
    {
        if (m_waitingUntilFinished && _inShake)
            return;

        if (!_inShake)
            _originalLocalPos = transform.localPosition; // only store once at start

        // Generate direction vector from angle
        float rad = Mathf.Deg2Rad * angle;
        _shakeNormal = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

        _currTime = 0f;
        m_duration = duration;
        m_frequency = frequency;
        m_strength = strength;

        _inShake = true;
    }

    private void Update()
    {
        if (_inShake && _shakeNormal != Vector2.zero)
        {
            if (_currTime <= m_duration)
            {
                _currTime += Time.deltaTime;
                _currShakeMultiplier = Mathf.Lerp(1f, 0f, _currTime / m_duration);

                Vector3 shakeOffset = _shakeNormal *
                                      (Mathf.Sin(Time.time * m_frequency) * m_strength * _currShakeMultiplier);

                // Apply as offset, not overwrite
                transform.localPosition = _originalLocalPos + shakeOffset;
            }
            else
            {
                // Restore original local position
                transform.localPosition = _originalLocalPos;
                _inShake = false;
            }
        }
    }
}
