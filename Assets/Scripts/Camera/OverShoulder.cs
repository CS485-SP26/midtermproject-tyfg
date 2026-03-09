using UnityEngine;
using UnityEngine.InputSystem;

namespace Cameras
{
    public class OverShoulder : Follow
    {
        Quaternion rotationOverride = Quaternion.identity;
        Vector3 rotationEuler = Vector3.zero;

        [SerializeField] float speed = 6f;

        public override Quaternion GetRotation()
        {
            return rotationOverride;
        }

        public void OnLook(InputValue value)
        {
            Vector2 input = value.Get<Vector2>();

            Vector3 euler = new Vector3(input.y, -input.x, 0f);
            euler *= 100f * Time.deltaTime;

            AddRotation(euler);
        }

        public void OnMove(InputValue value)
        {
            ResetRotation();
        }

        void LateUpdate()
        {
            Quaternion rOverride = rotationOverride;

            if (rotationEuler.magnitude > 0.001f)
            {
                rOverride = Quaternion.Euler(rotationEuler);
            }

            Quaternion rotation = follow.transform.rotation * rOverride;

            Vector3 position = rotation * offset;
            position += follow.transform.position;

            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                rotation,
                Time.deltaTime * speed
            );

            transform.position = Vector3.Lerp(
                transform.position,
                position,
                Time.deltaTime * speed
            );
        }

        public void AddRotation(Vector3 euler)
        {
            rotationEuler += euler;

            rotationEuler.x = Mathf.Clamp(rotationEuler.x, -30f, 60f);
            rotationEuler.y = Mathf.Repeat(rotationEuler.y, 360f);
        }

        public void ResetRotation()
        {
            rotationOverride = Quaternion.identity;
            rotationEuler = Vector3.zero;
        }
    }
}