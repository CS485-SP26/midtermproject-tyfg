using UnityEngine;
using UnityEngine.SceneManagement;

public class LeaveStoreButton : MonoBehaviour
{
    public void LeaveStore()
    {
        SceneManager.LoadScene("Scene1-FarmingSim");
    }
}
