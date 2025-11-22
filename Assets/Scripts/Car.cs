using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
public class Car : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float maxSpeed = 12f;
    [SerializeField] private float acceleration = 8f;
    [SerializeField] private float deceleration = 6f;

    [Header("Turning")]
    [SerializeField] private float maxTurnSpeed = 200f; // degrees per sec
    [SerializeField] private float turnAcceleration = 400f; // degrees/sec^2
    [SerializeField] private float turnDeceleration = 300f;

    [Header("Off-road settings")]
    [Tooltip("Layer(s) considered 'road' � make sure your road GameObjects are on this layer.")]
    [SerializeField] private LayerMask roadLayer = 0;
    [Range(0.1f, 1f)][SerializeField] private float offroadSpeedMultiplier = 0.6f;
    [Range(0.1f, 1f)][SerializeField] private float offroadAccelMultiplier = 0.6f;
    [Range(0.1f, 1f)][SerializeField] private float offroadTurnMultiplier = 0.75f;

    [Header("Nitro Settings")]
    public float currentNitro = 0f;
    public float nitroBoost = 1.5f;
    public float nitroBurnRate = 15f;
    public float maxNitro = 25f;
    public Image nitroFrontBar;
    public Image nitroBackBar;
    public float catchUpDelay = 0.5f;
    public float catchUpSpeed = 5f;

    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference nitroAction;

    private Rigidbody2D rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        nitroFrontBar.fillAmount = currentNitro / maxNitro;
        nitroBackBar.fillAmount = currentNitro / maxNitro;
    }

    private void OnEnable()
    {
        if (moveAction != null) moveAction.action.Enable();
        if (nitroAction != null) nitroAction.action.Enable();
    }

    private void OnDisable()
    {
        if (moveAction != null) moveAction.action.Disable();
        if (nitroAction != null) nitroAction.action.Disable();
    }

    private void FixedUpdate()
    {
        Vector2 input = moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
        bool nitroPressed = nitroAction != null && nitroAction.action.ReadValue<float>() > 0.5f;

        if (nitroPressed)
        {
            HandleNitro();
            UseNitro();
        }

        float turnInput = -input.x;

        // check if car is on the road (checks a point at the car's position)
        bool onRoad = Physics2D.OverlapPoint(transform.position, roadLayer) != null;

        // choose multipliers based on ground
        float currentMaxSpeed = maxSpeed * (onRoad ? 1f : offroadSpeedMultiplier);
        float currentAcceleration = acceleration * (onRoad ? 1f : offroadAccelMultiplier);
        float currentDeceleration = deceleration * (onRoad ? 1f : offroadAccelMultiplier); // use same reduction for decel
        float currentTurnAccelMultiplier = (onRoad ? 1f : offroadTurnMultiplier);

        // forward speed along car's forward vector
        float forwardSpeed = Vector2.Dot(rb.linearVelocity, transform.up);

        // apply forward/backward force based on whether we're under/over max
        float forwardForce = (forwardSpeed < currentMaxSpeed) ? currentAcceleration : -currentDeceleration;
        // apply force once (bug fix: your original code added force twice)
        rb.AddForce(transform.up * forwardForce, ForceMode2D.Force);

        // clamp velocity to currentMaxSpeed
        if (rb.linearVelocity.magnitude > currentMaxSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * currentMaxSpeed;

        // turning: scale target angular speed when offroad and reduce angular accel
        float targetAngular = turnInput * maxTurnSpeed * (onRoad ? 1f : offroadTurnMultiplier);

        float accel = (Mathf.Approximately(turnInput, 0f) ? turnDeceleration : turnAcceleration) * currentTurnAccelMultiplier;
        rb.angularVelocity = Mathf.MoveTowards(rb.angularVelocity, targetAngular, accel * Time.fixedDeltaTime);
    }

    public static IEnumerator setCarSlow(float speed,float duration)
    {
        Car car = FindObjectOfType<Car>();
        if (car != null && car.rb != null)
        {
            float originalSpeed = car.rb.linearVelocity.magnitude;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                car.rb.linearVelocity = car.rb.linearVelocity.normalized * Mathf.Lerp(originalSpeed, speed, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            car.rb.linearVelocity = car.rb.linearVelocity.normalized * speed;
        }
    }

    public void HandleNitro()
    {
        nitroFrontBar.fillAmount = currentNitro/ maxNitro;
        if (nitroBackBar.fillAmount < nitroFrontBar.fillAmount)
        {
            nitroBackBar.fillAmount = currentNitro/ maxNitro;
        }
        else
        {
            nitroBackBar.fillAmount = Mathf.Lerp(
                nitroBackBar.fillAmount,
                currentNitro / maxNitro,
                Time.deltaTime * catchUpSpeed
            );
        }
    }

    public void AddNitro(float amount)
    {
        currentNitro = Mathf.Clamp(currentNitro + amount, 0f, maxNitro);
    }
    public bool UseNitro()
    {
        if (currentNitro > 0f)
        {
            currentNitro -= nitroBurnRate * Time.fixedDeltaTime;
            rb.AddForce(transform.up * acceleration * nitroBoost, ForceMode2D.Force);
            return true;
        }
        return false;
    }
}
