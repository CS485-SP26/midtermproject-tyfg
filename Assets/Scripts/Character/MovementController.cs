using System.Collections.Generic;
using UnityEngine;

/* 
* Handles character movement and jumping, including:
*   - Horizontal movement with acceleration and max speed
*   - Multiple jumps (e.g. double jump)
*   - Crouching with collider adjustments
*   - Ground detection using collision contacts and a GroundProximity trigger
*   - Jump queueing for more responsive input
* Exposes:
*   - Move(Vector2 input)
*   - Jump()
*   - GetHorizontalSpeedPercent()
*   - ToggleWalk()
*   - SetSprint(bool)
*   - ToggleCrouch()
*   - IsGrounded (property)
*   - VerticalVelocity (property)
*   - IsNearGround (property)
*   - IsCrouching (property)
*   - CanJump() (method)
*/

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
        // How quickly velocity moves toward the desired movement speed.
        [SerializeField] protected float acceleration = 10f;
        // Fixed low speed used while walk mode is toggled.
        [SerializeField] protected float walkSpeed = 2f;
        // Default movement speed used for normal jogging.
        [SerializeField] protected float baseSpeed = 4f;
        // Multiplier applied to base speed while sprinting.
        [SerializeField] protected float sprintMultiplier = 1.5f;
        //[SerializeField] float rotationSpeed = 10f;
        // Optional hard cap for horizontal velocity (legacy support field).
        [SerializeField] protected float maxVelocity = 6f;

        // Jump settings
        [Header("Jump Settings")]
        // Maximum number of jumps before touching ground again.
        [SerializeField] protected int maxJumps = 2;
        // Upward impulse used for each jump.
        [SerializeField] protected float jumpForce = 6f;
        // Multiplier applied to movement speed while crouching.
        [SerializeField] protected float crouchMultiplier = 0.6f;
        // Optional helper component that tracks whether ground is near.
        [SerializeField] protected GroundProximity groundProximity;
        // Layer mask used to decide which colliders count as ground.
        [SerializeField] protected LayerMask groundLayers = ~0;
        // Extra tag names that should also count as ground.
        [SerializeField] protected string[] extraGroundTags;

        // Collider adjustments for crouching
        [Header("Crouch Settings")]
        // Capsule collider that gets resized when crouching.
        [SerializeField] protected CapsuleCollider capsule;
        // Original standing collider height.
        [SerializeField] protected float standingHeight = 2f;
        // Target collider height while crouching.
        [SerializeField] protected float crouchingHeight = 1.2f;
        // Original standing collider center Y value.
        [SerializeField] protected float standingCenterY = 1f;
        // Cached crouching collider center Y value.
        [SerializeField] protected float crouchingCenterY = 0.6f;

        // Jump queueing
        // If true, jump input can be queued and consumed in FixedUpdate.
        [SerializeField] protected bool allowJumpQueue = true;

        // Internal state
        // Rigidbody used for all physics movement.
        protected Rigidbody rb;
        // Current movement input from the player/controller.
        protected Vector2 moveInput;
        // True when jump input was pressed and should be applied by physics step.
        protected bool jumpQueued;
        // Set of colliders currently considered valid ground contacts.
        protected readonly HashSet<Collider> groundedColliders = new HashSet<Collider>();
        // Remaining jumps available before needing ground contact.
        protected int jumpsRemaining;

        // Runtime movement mode flags.
        protected bool isSprinting = false;
        protected bool isWalking = false;
        protected bool isCrouching = false;

        // -------------------------
        // Initialization
        // -------------------------
        // Cache required components and initialize runtime state at startup.
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
        // Receives movement input from another script (usually PlayerController).
        public void Move(Vector2 input)
        {
            moveInput = input;
        }

        // Queues a jump request, optionally requiring grounded state first.
        public virtual void Jump()
        {
            if (!allowJumpQueue && !IsGrounded)
                return;

            jumpQueued = true;
        }

        // Basic animator helper: returns 1 when there is movement input, else 0.
        public virtual float GetHorizontalSpeedPercent()
        {
            if (moveInput.sqrMagnitude < 0.01f)
                return 0f;

            return 1f;
        }

        // -------------------------
        // Extra movement features
        // -------------------------
        // Toggles walking mode on/off.
        public void ToggleWalk()
        {
            isWalking = !isWalking;
        }

        // Sets sprinting mode from external input.
        public void SetSprint(bool sprinting)
        {
            isSprinting = sprinting;
        }

        // Toggles crouching and resizes the capsule while keeping feet at same world Y.
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

        // Changes capsule height while preserving bottom position to avoid sinking/floating.
        protected void SetCapsuleHeightKeepingBottom(float targetHeight)
        {
            // Prevent invalid or near-zero collider heights.
            float clampedHeight = Mathf.Max(0.01f, targetHeight);

            // Bottom = centerY - halfHeight.
            float currentBottom = capsule.center.y - (capsule.height * 0.5f);
            // New center keeps bottom fixed while changing height.
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
        // Checks new collisions for walkable/grounded contact.
        protected virtual void OnCollisionEnter(Collision collision)
        {
            EvaluateGroundCollision(collision);
        }
        
        // Re-checks active collisions to keep grounded state current.
        protected virtual void OnCollisionStay(Collision collision)
        {
            EvaluateGroundCollision(collision);
        }

        // Removes collider from grounded set when contact ends.
        protected virtual void OnCollisionExit(Collision collision)
        {
            if (IsGroundCollider(collision.collider))
            {
                groundedColliders.Remove(collision.collider);
            }
        }

        // Evaluates contact normals to decide whether a collision counts as standing on ground.
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

        // Returns true if collider layer/tag configuration should count as ground.
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

        // Internal grounded check based on active ground colliders.
        bool IsGroundedInternal()
        {
            return groundedColliders.Count > 0;
        }

        // Public state properties used by other systems (animation, input, gameplay).
        public bool IsGrounded => IsGroundedInternal();
        public float VerticalVelocity => rb.linearVelocity.y;
        public bool IsNearGround => groundProximity && groundProximity.nearGround;
        public bool IsCrouching => isCrouching;
        public bool HasMovementInput => moveInput.sqrMagnitude > 0.01f;

        // Backwards-compatible wrappers while transitioning call sites.
        // Compatibility wrappers to avoid breaking older references.
        public bool IsGroundedState() => IsGrounded;
        public float VerticalVelocityState() => VerticalVelocity;
        public bool NearGround() => IsNearGround;
        public bool IsCrouchingState() => IsCrouching;

        // Returns whether this controller currently has jumps available.
        public virtual bool CanJump()
        {
            return jumpsRemaining > 0;
        }

        // -------------------------
        // Physics Update (EMPTY NOW)
        // -------------------------
        // Intentionally empty in base class; subclasses perform actual physics movement.
        protected virtual void FixedUpdate()
        {
            // PhysicsMovement now handles all physics.
        }
    }
}
