using UnityEngine;

namespace Character
{
    public class CameraFollow : MonoBehaviour
    {
        // Target object the camera tracks.
        [SerializeField] public GameObject player;
        // World-space offset applied from player to camera.
        [SerializeField] private Vector3 offset = new(0f, 0f, -3f);

        // Validates that a follow target exists.
        void Start()
        {
            Debug.Assert(player, "CameraFollow requires a player (GameObject).");
        }

        // Runs after movement updates to reduce visible follow jitter.
        void LateUpdate()
        {
            transform.position = player.transform.position + offset;
        }
    }
}
