using System.Collections;
using System.Runtime.InteropServices;
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
    [Tooltip("Layer(s) considered 'road' — make sure your road GameObjects are on this layer.")]
    [SerializeField] private LayerMask roadLayer = 0;
    [Range(0.1f, 1f)][SerializeField] private float offroadSpeedMultiplier = 0.6f;
    [Range(0.1f, 1f)][SerializeField] private float offroadAccelMultiplier = 0.6f;
    [Range(0.1f, 1f)][SerializeField] private float offroadTurnMultiplier = 0.75f;

    [Header("Nitro Settings")]
    public float currentNitro = 0f;
    public float nitroBoost = 1.5f;
    public float nitroBurnRate = 15f;
    public float maxNitro = 25f;
    public float catchUpDelay = 0.5f;
    public float catchUpSpeed = 5f;
    public float nitroRechargeRate = 1f;
    [Header("Nitro Ui")]
    public Image nitroFrontBar;
    public Image nitroBackBar;
    public DoubleTxt nitroTxt;
    public float flickerRate = 0.2f;
    [HideInInspector] public float slowSpeed = 1f;

    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference nitroAction;
    private float scoreTimer = 0f;

    private Rigidbody2D rb;

    // --- New fields for remembering player's nitro intent ---
    private bool nitroRequested = false;         // true while player is requesting nitro (holding it)
    private bool previousNitroPressed = false;   // for edge detection

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (nitroFrontBar != null) nitroFrontBar.fillAmount = currentNitro / maxNitro;
        if (nitroBackBar != null) nitroBackBar.fillAmount = currentNitro / maxNitro;
    }

    private void OnEnable()
    {
        if (moveAction != null) moveAction.action.Enable();
        if (nitroAction != null) nitroAction.action.Enable();
    }

    public void OnGameOver()
    {
        transform.gameObject.SetActive(false);
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

        // --- NEW: track request / button edge ---
        if (nitroPressed && !previousNitroPressed)
        {
            // button just pressed -> start requesting nitro
            nitroRequested = true;
        }
        else if (!nitroPressed)
        {
            // button released -> cancel request
            nitroRequested = false;
        }
        previousNitroPressed = nitroPressed;
        // ------------------------------------------------

        // Check road
        bool onRoad = Physics2D.OverlapPoint(transform.position, roadLayer) != null;

        // Determine whether nitro should be active this frame:
        // (activate if the player is requesting nitro and there is fuel)
        bool nitroActiveThisFrame = nitroRequested && currentNitro > 0f;

        if (nitroActiveThisFrame)
        {
            // Show UI flicker/active state
            if (nitroTxt != null) nitroTxt.setBackActive(Mathf.FloorToInt(Time.time / 0.1f) % 2 == 0);
            HandleNitro();
        }
        else
        {
            // Recharge nitro while on road (only when not actively burning)
            AddNitro(nitroRechargeRate * Time.fixedDeltaTime * (onRoad ? 1f : 0f));
            if (nitroTxt != null) nitroTxt.setBackActive(false);
        }

        // choose multipliers based on ground
        float currentMaxSpeed = maxSpeed * (onRoad ? 1f : offroadSpeedMultiplier);
        float currentAcceleration = acceleration * (onRoad ? 1f : offroadAccelMultiplier);
        float currentDeceleration = deceleration * (onRoad ? 1f : offroadAccelMultiplier); // use same reduction for decel
        float currentTurnAccelMultiplier = (onRoad ? 1f : offroadTurnMultiplier);

        // If nitro is active, temporarily increase allowable top speed so boost can take effect
        if (nitroActiveThisFrame)
        {
            currentMaxSpeed *= nitroBoost; // allow higher top speed while boosting
        }

        // forward speed along car's forward vector
        float forwardSpeed = Vector2.Dot(rb.linearVelocity, transform.up);

        // apply forward/backward force based on whether we're under/over max
        float forwardForce = (forwardSpeed < currentMaxSpeed) ? currentAcceleration : -currentDeceleration;

        // Apply forward force (base)
        rb.AddForce(transform.up * forwardForce * slowSpeed, ForceMode2D.Force);

        // Apply nitro boost AFTER base force, using UseNitro which will apply a partial boost if needed
        if (nitroActiveThisFrame)
        {
            UseNitro(); // now UseNitro will only apply the boost for available nitro
        }

        // clamp velocity to currentMaxSpeed (which already accounts for nitroActiveThisFrame)
        if (rb.linearVelocity.magnitude > currentMaxSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * currentMaxSpeed;

        // (turning code)
        float turnInput = -input.x;
        float targetAngular = turnInput * maxTurnSpeed * (onRoad ? 1f : offroadTurnMultiplier);
        float accel = (Mathf.Approximately(turnInput, 0f) ? turnDeceleration : turnAcceleration) * currentTurnAccelMultiplier;
        rb.angularVelocity = Mathf.MoveTowards(rb.angularVelocity, targetAngular, accel * Time.fixedDeltaTime);

        // score timer handling
        scoreTimer += Time.fixedDeltaTime;
        if (scoreTimer >= 1f)
        {
            scoreTimer = 0f;
            ScoreManager.instance.AddScore(1 * (onRoad ? 1 : 0));
        }
    }

    public static void setCarSlow(float speed)
    {
        Car car = FindObjectOfType<Car>();
        if (car != null && car.rb != null)
            car.rb.linearVelocity *= speed;
    }

    public void HandleNitro()
    {
        if (nitroFrontBar != null) nitroFrontBar.fillAmount = currentNitro / maxNitro;

        if (nitroBackBar != null)
        {
            if (nitroBackBar.fillAmount < nitroFrontBar.fillAmount)
            {
                nitroBackBar.fillAmount = currentNitro / maxNitro;
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
    }

    public void AddNitro(float amount)
    {
        currentNitro = Mathf.Clamp(currentNitro + amount, 0f, maxNitro);
        HandleNitro();
    }

    public bool UseNitro()
    {
        // How much nitro we want to consume this physics frame
        float desiredBurn = nitroBurnRate * Time.fixedDeltaTime;

        if (currentNitro <= 0f)
        {
            currentNitro = 0f;
            return false;
        }

        // Consume up to desiredBurn, but allow partial consumption if not enough nitro remains
        float actualBurn = Mathf.Min(currentNitro, desiredBurn);
        currentNitro -= actualBurn;

        // Calculate a scalar [0..1] representing how much boost to apply this frame
        float burnRatio = (desiredBurn > 0f) ? (actualBurn / desiredBurn) : 0f;

        if (burnRatio <= 0f)
        {
            // Nothing to apply
            currentNitro = Mathf.Max(0f, currentNitro);
            return false;
        }

        // Apply boost scaled by burnRatio so if nitro runs out mid-frame we still get the remaining push
        float boostForce = acceleration * nitroBoost * burnRatio;
        rb.AddForce(transform.up * boostForce, ForceMode2D.Force);

        // Update UI
        HandleNitro();

        return true;
    }
}
