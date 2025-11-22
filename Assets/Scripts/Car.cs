using UnityEngine;
using UnityEngine.InputSystem;

public class Car : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float maxSpeed = 12f;
    [SerializeField] private float acceleration = 8f;
    [SerializeField] private float deceleration = 6f;

    [Header("Turning")]
    [SerializeField] private float maxTurnSpeed = 200f; // degrees per sec
    [SerializeField] private float turnAcceleration = 400f; // degrees/sec^2 (applied as degrees/sec change per sec)
    [SerializeField] private float turnDeceleration = 300f;

    [SerializeField] private InputActionReference moveAction;

    private Rigidbody2D rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        moveAction.action.Enable();
    }

    private void OnDisable()
    {
        moveAction.action.Disable();
    }

    private void FixedUpdate()
    {
        Vector2 input = moveAction.action.ReadValue<Vector2>();
        float turnInput = -input.x;

        float forwardSpeed = Vector2.Dot(rb.linearVelocity, transform.up);

        float forwardForce;
        if (forwardSpeed < maxSpeed)
            forwardForce = acceleration;
        else
            forwardForce = -deceleration;

        rb.AddForce(transform.up * forwardForce, ForceMode2D.Force);
        rb.AddForce(transform.up * forwardForce, ForceMode2D.Force);

        if (rb.linearVelocity.magnitude > maxSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;

        float targetAngular = turnInput * maxTurnSpeed;

        float accel = (Mathf.Approximately(turnInput, 0f) ? turnDeceleration : turnAcceleration);
        rb.angularVelocity = Mathf.MoveTowards(rb.angularVelocity, targetAngular, accel * Time.fixedDeltaTime);
    }
}
