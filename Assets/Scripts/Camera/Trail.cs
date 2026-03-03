using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;
using Core;
using Unity.VisualScripting;

namespace Cameras {
    
    public class Trail : Follow
    {
        FollowTrail followTrail;

        void Start()
        {
            followTrail = this.follow.GetComponent<FollowTrail>();
        }

        protected override void Update()
        {
            
        }

        void FixedUpdate()
        {
            Vector3 newPos = followTrail.Peek() + offset;
            this.transform.position = Vector3.Lerp(this.transform.position, newPos, Time.fixedDeltaTime);
        }
    }
}