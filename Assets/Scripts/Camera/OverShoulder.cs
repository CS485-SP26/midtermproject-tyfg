using UnityEngine;
using UnityEngine.InputSystem;

namespace Cameras {
    public class OverShoulder : Follow
    {
        Quaternion rotationOverride = Quaternion.identity;
        Vector3 rotationEuler = Vector3.zero;
        [SerializeField] float speed;

        public override Quaternion GetRotation()
        {
            return rotationOverride;
        }

        public void OnLook(InputValue value)
        {
            Vector2 input = value.Get<Vector2>();
            Vector3 euler = new Vector3(input.y, -input.x, 0f);
            euler *= 100f;            
            AddRotation(euler);
        }

        public void OnMove(InputValue value)
        {
            ResetRotation();
        }

        // Update is called once per frame
        void Update()
        {
            Quaternion rOverride = rotationOverride;
            if (rotationEuler.magnitude > 0.001f)
            {
                rOverride = Quaternion.Euler(rotationEuler);
            }
            Quaternion rotation = follow.transform.rotation * rOverride;
            Vector3 position = rotation * offset;
            position += follow.transform.position;

            transform.rotation = Quaternion.Lerp(transform.rotation,
                rotation,
                Time.deltaTime * speed);
            transform.position = Vector3.Lerp(transform.position, 
                position, 
                Time.deltaTime * speed);
        }

        public void AddRotation(Vector3 euler)
        {
            rotationEuler += euler;
            rotationEuler.x = Mathf.Clamp(rotationEuler.x, -20f, 20f);
        }

        public void AddRotation(Quaternion rotation)
        {
            rotationOverride *= rotation;
        }

        public void ResetRotation()
        {
            rotationOverride = Quaternion.identity;
        }
    }
}
