using UnityEngine;
using UnityEngine.SceneManagement;

public class ShopEnterZone : MonoBehaviour
{
    // Button shown while player is inside shop trigger area.
    [SerializeField] private GameObject enterStoreButton;
    // Scene name loaded when entering store.
    [SerializeField] private string shopSceneName = "Scene2-Store";

    // Hides store button on startup.
    void Start()
    {
        if (enterStoreButton != null)
        {
            enterStoreButton.SetActive(false);
        }
    }

    // Shows enter-store button when player enters trigger.
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && enterStoreButton != null)
        {
            enterStoreButton.SetActive(true);
        }
    }

    // Hides enter-store button when player exits trigger.
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && enterStoreButton != null)
        {
            if (enterStoreButton != null)
            {
                enterStoreButton.SetActive(false);
            }
        }
    }

    // UI click handler to load the store scene.
    public void enterStore()
    {
        SceneManager.LoadScene(shopSceneName);
    }
}
