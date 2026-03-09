using UnityEngine;

public class FireworksZone : MonoBehaviour
{
    [SerializeField] private GameObject fireworksButton;
    [SerializeField] private GameObject rocketPrefab;

    void Start()
    {
        if (fireworksButton != null)
            fireworksButton.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && fireworksButton != null)
            fireworksButton.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && fireworksButton != null)
            fireworksButton.SetActive(false);
    }

    public void LaunchFireworks()
    {
         Debug.Log("Fireworks button pressed");

        Vector3 spawnPos = transform.position + Vector3.up * 1f;

        Instantiate(rocketPrefab, spawnPos, Quaternion.identity);
    }
}