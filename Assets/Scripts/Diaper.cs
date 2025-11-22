using UnityEngine;

public class Diaper : MonoBehaviour
{
    public Transform player;

    [Header("Speed Settings")]
    public float baseSpeed = 3f;         // constant follow speed
    public float maxSpeed = 8f;          // speed when far away
    public float accelerateDistance = 3f; // if further than this → speed up
    public float acceleration = 5f;      // how fast it blends to higher speed

    private Rigidbody2D rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (player == null) return;

        // Target with a small Y offset
        Vector2 targetPos = player.position;
        targetPos.y += 0.5f;

        Vector2 dir = (targetPos - (Vector2)transform.position);
        float dist = dir.magnitude;

        if (dist < 0.01f)
            return; // Avoid jitter

        dir.Normalize();

        // Determine dynamic speed
        float targetSpeed = baseSpeed;

        if (dist > accelerateDistance)
        {
            // smoothly blend towards max speed
            targetSpeed = Mathf.Lerp(baseSpeed, maxSpeed, (dist - accelerateDistance) * 0.5f);
        }

        // Clamp speed so it never exceeds max
        targetSpeed = Mathf.Min(targetSpeed, maxSpeed);

        // Move using Rigidbody2D velocity (more stable for constant motion)
        rb.linearVelocity = dir * targetSpeed;
    }
}
