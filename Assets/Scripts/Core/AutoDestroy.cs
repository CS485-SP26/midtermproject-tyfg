using UnityEngine;

namespace Core {
    public class AutoDestroy : MonoBehaviour
    {
        [SerializeField] private float lifespan = 5f;
        // Update is called once per frame
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
