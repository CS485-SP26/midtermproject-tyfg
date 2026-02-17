using System.Collections.Generic;
using UnityEngine;

namespace Character
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class MovementController : MonoBehaviour
    {
        // -------------------------
        // Movement Settings
        // -------------------------
        [Header("Movement Settings")]
        [SerializeField] protected float acceleration = 10f;
        [SerializeField] protected float walkSpeed = 2f;
        [SerializeField] protected float baseSpeed = 4f;
        [SerializeField] protected float sprintMultiplier = 1.5f;
        //[SerializeField] float rotationSpeed = 10f;
        [SerializeField] protected float maxVelocity = 6f;

        // Jump settings
        [Header("Jump Settings")]
        [SerializeField] protected int maxJumps = 2;
        [SerializeField] protected float jumpForce = 6f;
        [SerializeField] protected float crouchMultiplier = 0.6f;
        [SerializeField] protected GroundProximity groundProximity;
        [SerializeField] protected LayerMask groundLayers = ~0;
        [SerializeField] protected string[] extraGroundTags;

        // Collider adjustments for crouching
        [Header("Crouch Settings")]
        [SerializeField] protected CapsuleCollider capsule;
        [SerializeField] protected float standingHeight = 2f;
        [SerializeField] protected float crouchingHeight = 1.2f;
        [SerializeField] protected float standingCenterY = 1f;
        [SerializeField] protected float crouchingCenterY = 0.6f;

        // Jump queueing
        [SerializeField] protected bool allowJumpQueue = true;

        // Internal state
        protected Rigidbody rb;
        protected Vector2 moveInput;
        protected bool jumpQueued;
        // added in with chatgpt 
        protected readonly HashSet<Collider> groundedColliders = new HashSet<Collider>();
        // Removed with chatgpt protected int groundContacts;
        protected int jumpsRemaining;

        protected bool isSprinting = false;
        protected bool isWalking = false;
        protected bool isCrouching = false;

        // -------------------------
        // Initialization
        // -------------------------
        protected virtual void Start()
        {
            rb = GetComponent<Rigidbody>();
            Debug.Assert(rb, "MovementController requires a Rigidbody");

            if(!capsule)
                capsule = GetComponent<CapsuleCollider>();
            Debug.Assert(rb, "MovementController requires a CapsuleCollider for crouching.");

            if(!groundProximity)
                groundProximity = GetComponentInChildren<GroundProximity>();

            // Sync standing values with the actual collider at startup so crouch math
            // uses the true setup from the prefab/scene.
            standingHeight = capsule.height;
            standingCenterY = capsule.center.y;

            jumpsRemaining = maxJumps;
        }

        // -------------------------
        // Midterm API (PlayerController calls these)
        // -------------------------
        public void Move(Vector2 input)
        {
            moveInput = input;
        }

        public virtual void Jump()
        {
            if (!allowJumpQueue && !IsGrounded)
                return;

            jumpQueued = true;
        }

        public virtual float GetHorizontalSpeedPercent()
        {
            if (moveInput.sqrMagnitude < 0.01f)
                return 0f;

            return 1f;
        }

        // -------------------------
        // Extra movement features
        // -------------------------
        public void ToggleWalk()
        {
            isWalking = !isWalking;
        }

        public void SetSprint(bool sprinting)
        {
            isSprinting = sprinting;
        }

        public virtual void ToggleCrouch()
        {
            if (!IsGrounded)
                return;
            if (!capsule)
                return;

            isCrouching = !isCrouching;

            if (isCrouching)
            {
                SetCapsuleHeightKeepingBottom(crouchingHeight);
            }
            else
            {
                SetCapsuleHeightKeepingBottom(standingHeight);
            }
        }

        protected void SetCapsuleHeightKeepingBottom(float targetHeight)
        {
            float clampedHeight = Mathf.Max(0.01f, targetHeight);

            float currentBottom = capsule.center.y - (capsule.height * 0.5f);
            float targetCenterY = currentBottom + (clampedHeight * 0.5f);

            if (Mathf.Approximately(clampedHeight, crouchingHeight))
                crouchingCenterY = targetCenterY;

            capsule.height = clampedHeight;
            Vector3 center = capsule.center;
            center.y = targetCenterY;
            capsule.center = center;
        }

        // -------------------------
        // Ground Detection
        // -------------------------
        protected virtual void OnCollisionEnter(Collision collision)
        {
            EvaluateGroundCollision(collision);
        }
        
        protected virtual void OnCollisionStay(Collision collision)
        {
            EvaluateGroundCollision(collision);
        }

        protected virtual void OnCollisionExit(Collision collision)
        {
            if (IsGroundCollider(collision.collider))
            {
                groundedColliders.Remove(collision.collider);
            }
        }

        protected virtual void EvaluateGroundCollision(Collision collision)
        {
            if (!IsGroundCollider(collision.collider))
                return;
            
            bool hasGroundLikeContact = false;
            foreach (ContactPoint contact in collision.contacts)
            {
                if ( contact.normal.y > 0.5f)
                {
                    hasGroundLikeContact = true;
                    break;
                }
            }

            if (!hasGroundLikeContact)
            {
                groundedColliders.Remove(collision.collider);
                return;
            }

            groundedColliders.Add(collision.collider);
            
            if (rb.linearVelocity.y < 0f)
            {
                rb.linearVelocity = new Vector3(
                rb.linearVelocity.x,
                0f,
                rb.linearVelocity.z
                );
            }

            // Refill jumps on grounded contact once upward motion has ended.
            // This avoids getting stuck at 0 jumps if a collider was never removed from the set.
            if (rb.linearVelocity.y <= 0f)
            {
                jumpsRemaining = maxJumps;
            }
        }

        protected bool IsGroundCollider(Collider collider)
        {
            if (!collider)
                return false;

            int layerBit = 1 << collider.gameObject.layer;
            if ((groundLayers.value & layerBit) != 0)
                return true;

            if (collider.CompareTag("Ground"))
                return true;

            if (extraGroundTags == null || extraGroundTags.Length == 0)
                return false;

            foreach (string tag in extraGroundTags)
            {
                if (!string.IsNullOrWhiteSpace(tag) && collider.CompareTag(tag))
                    return true;
            }

            return false;
        }

        bool IsGroundedInternal()
        {
            return groundedColliders.Count > 0;
        }

        public bool IsGrounded => IsGroundedInternal();
        public float VerticalVelocity => rb.linearVelocity.y;
        public bool IsNearGround => groundProximity && groundProximity.nearGround;
        public bool IsCrouching => isCrouching;

        // Backwards-compatible wrappers while transitioning call sites.
        public bool IsGroundedState() => IsGrounded;
        public float VerticalVelocityState() => VerticalVelocity;
        public bool NearGround() => IsNearGround;
        public bool IsCrouchingState() => IsCrouching;

        public virtual bool CanJump()
        {
            return jumpsRemaining > 0;
        }

        // -------------------------
        // Physics Update (EMPTY NOW)
        // -------------------------
        protected virtual void FixedUpdate()
        {
            // PhysicsMovement now handles all physics.
        }
    }
}
