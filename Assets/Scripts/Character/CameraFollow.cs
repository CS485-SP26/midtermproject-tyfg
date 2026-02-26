using UnityEngine;

/*
* This script makes the camera follow the player GameObject with a specified offset. It ensures that the camera's position is updated in LateUpdate to reduce jitter during player movement.
* Exposes:
*   - player: The GameObject that the camera will follow.
*   - offset: The world-space offset from the player to the camera.
* Requires:
*   - The player GameObject must be assigned in the inspector for the camera to follow it.
*/
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
