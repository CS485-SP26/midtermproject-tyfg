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
            if (!allowJumpQueue && !IsGrounded())
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
            if (!IsGrounded())
                return;
            if (!capsule)
                return;

            isCrouching = !isCrouching;

            if (isCrouching)
            {
                capsule.height = crouchingHeight;
                capsule.center = new Vector3(0, crouchingCenterY, 0);
            }
            else
            {
                capsule.height = standingHeight;
                capsule.center = new Vector3(0, standingCenterY, 0);
            }
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
            if (collision.gameObject.CompareTag("Ground")) 
            {
                groundedColliders.Remove(collision.collider);
            }
        }

        protected virtual void EvaluateGroundCollision(Collision collision)
        {
            if (!collision.gameObject.CompareTag("Ground"))
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

            bool justLanded = groundedColliders.Add(collision.collider);

            if(!justLanded)
                return;
            
            if (rb.linearVelocity.y < 0f)
            {
                rb.linearVelocity = new Vector3(
                rb.linearVelocity.x,
                0f,
                rb.linearVelocity.z
                );
            }
            
            jumpsRemaining = maxJumps;
        }

        bool IsGrounded()
        {
            return groundedColliders.Count > 0;
        }

        public bool IsGroundedState() => IsGrounded();
        public float VerticalVelocity() => rb.linearVelocity.y;
        public bool NearGround() => groundProximity && groundProximity.nearGround;
        public bool IsCrouchingState() => isCrouching;

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