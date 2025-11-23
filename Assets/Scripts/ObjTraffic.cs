using UnityEngine;

public class ObjTraffic : MonoBehaviour
{
    public int PointValue = 10;
    public float nitroBoost = 1.5f;
    public float slowspeedMultiplier = 0.5f;
    public GameObject particle;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("FrontBumper"))
        {
            Car.setCarSlow(slowspeedMultiplier);
        }
        if (collision.CompareTag("BackBumper"))
        {
            ScoreManager.instance.AddScore(PointValue);
        }
        if (particle !=null)
        {
        Instantiate(particle, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }
}
