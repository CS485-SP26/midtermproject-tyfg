using UnityEngine;
using UnityEngine.SceneManagement;

public class ShopEnterZone : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private GameObject enterStoreButton;
    [SerializeField] private string shopSceneName = "Scene2-Store";
    void Start()
    {
        if (enterStoreButton != null)
        {
            enterStoreButton.SetActive(false);
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && enterStoreButton != null)
        { 
            enterStoreButton.SetActive(true);    
        }
    }

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

    public void enterStore()
    {
        SceneManager.LoadScene(shopSceneName);
    }
}
