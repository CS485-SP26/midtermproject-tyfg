using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core
{
    public class LoadScene:MonoBehaviour
    {
        public void LoadScenebyName(string name)
        {
            SceneManager.LoadScene(name);
        }
    }
}