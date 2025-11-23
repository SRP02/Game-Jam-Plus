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
            AudioManager.Main.PlaySound("Crash", 0.2f, Random.Range(1.5f,2.5f));
            Instantiate(particle, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }
}
