using UnityEngine;
using UnityEngine.InputSystem;

namespace Cameras {
    public class Free : Follow
    {
        Vector3 movement;
        float moveSpeed = 1f;
        public override Quaternion GetRotation()
        {
            return Quaternion.identity;
        }

        public void OnMove(InputValue value)
        {
            Vector2 inputDir = value.Get<Vector2>();
            movement = new Vector3(inputDir.x, 0f, inputDir.y);
        }

        void Update()
        {
            offset += movement * moveSpeed * Time.deltaTime;
            base.Update();
        }
    }
}