using UnityEngine;
using UnityEngine.SceneManagement;

public class LeaveStoreButton : MonoBehaviour
{
    // UI click handler to return from store scene to farm scene.
    public void LeaveStore()
    {
        SceneManager.LoadScene("Scene1-FarmingSim");
    }
}
