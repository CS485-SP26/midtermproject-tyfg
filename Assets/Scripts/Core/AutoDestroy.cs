using UnityEngine;

/*
* A simple script that destroys the GameObject after a specified lifespan.
*/

namespace Core {
    public class AutoDestroy : MonoBehaviour
    {
        // Lifetime in seconds before this object is destroyed.
        [SerializeField] private float lifespan = 5f;

        // Counts down lifespan and destroys object once timer expires.
        void Update()
        {
            lifespan -= Time.deltaTime;
            if (lifespan < 0f)
            {
                Destroy(gameObject);
            }
        }
    }
}
