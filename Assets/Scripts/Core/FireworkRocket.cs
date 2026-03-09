using UnityEngine;

public class FireworkRocket : MonoBehaviour
{
    [SerializeField] private ParticleSystem explosion;
    [SerializeField] private float launchForce = 15f;
    [SerializeField] private float explodeTime = 2f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Launch rocket upward
        rb.AddForce(Vector3.up * launchForce, ForceMode.Impulse);

        // Explode after delay
        Invoke(nameof(Explode), explodeTime);
    }

    void Explode()
    {
        if (explosion != null)
        {
            Instantiate(explosion, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}