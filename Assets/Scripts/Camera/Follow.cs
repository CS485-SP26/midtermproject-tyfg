using Cameras;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cameras 
{
    public class Follow : MonoBehaviour
    {
        [SerializeField] protected Vector3 offset;
        [SerializeField] protected GameObject follow;
        public virtual Quaternion GetRotation() { return transform.rotation; }

        protected virtual void Update()
        {
            transform.position = follow.transform.position + offset;
        }
    }
}