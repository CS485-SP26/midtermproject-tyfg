using UnityEngine;
using UnityEngine.InputSystem;

namespace Cameras
{
    public class AIVersion : Follow
    {
        [SerializeField] float rotationSpeed = 200f; // degrees per second
        [SerializeField] float moveSpeed = 5f;
        [SerializeField] float maxPitch = 20f;

        float yaw = 0f;
        float pitch = 0f;
        Vector3 movementInput = Vector3.zero;

        void Start()
        {
            // Initialize yaw/pitch from current rotation
            Vector3 angles = transform.eulerAngles;
            yaw = angles.y;
            pitch = angles.x;
        }

        public override Quaternion GetRotation()
        {
            // Return the current rotation without roll
            return Quaternion.Euler(pitch, yaw, 0f);
        }

        public void OnLook(InputValue value)
        {
            Vector2 input = value.Get<Vector2>();
            yaw += input.x * rotationSpeed * Time.deltaTime;
            pitch -= input.y * rotationSpeed * Time.deltaTime; // invert Y if desired
            pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);
        }

        public void OnMove(InputValue value)
        {
            Vector2 input = value.Get<Vector2>();
            // Store movement input for use in Update
            movementInput = new Vector3(input.x, 0f, input.y);
        }

        void Update()
        {
            // Apply rotation relative to follow target
            Quaternion targetRotation = follow.transform.rotation * Quaternion.Euler(pitch, yaw, 0f);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * 10f);

            // Apply position: follow target plus offset
            Vector3 targetPosition = transform.rotation * offset + follow.transform.position;

            // Optionally add movement relative to camera forward/right
            Vector3 moveWorld = transform.right * movementInput.x + transform.forward * movementInput.z;
            targetPosition += moveWorld * moveSpeed * Time.deltaTime;

            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 10f);
        }
    }
}

