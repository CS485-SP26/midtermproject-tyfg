using UnityEngine;

/*
* A MovementController that uses Rigidbody physics for movement and jumping.
* Movement is applied by calculating a desired velocity based on input and applying a force to reach that -
* velocity, allowing for acceleration and more natural movement.
* Jumping is applied by setting the vertical velocity to zero and applying an impulse force upwards.
* Velocity is clamped to a maximum speed to prevent excessive velocity from physics interactions.
* Rotation is applied by smoothly rotating towards the input direction.
* Exposes the same public interface as MovementController, but with physics-based implementations.
*/
namespace Character
{
    // Physics-based movement controller built on top of MovementController.
    //
    // Quaternion:
    // Represents 3D rotation in a stable way (avoids gimbal lock from Euler angles).
    //
    // Quaternion.Slerp(from, to, t):
    // Smoothly rotates from one Quaternion to another along the shortest arc.
    // t is typically between 0 and 1, where 0 is "stay at from" and 1 is "reach to".
    public class PhysicsMovement : MovementController
    {
        // Linear damping on the Rigidbody (how quickly velocity naturally slows).
        [SerializeField] float drag = 0.5f;
        // Turn speed used when rotating toward movement direction.
        [SerializeField] float physicsRotationSpeed = 10f;

        // Initializes base movement state and applies physics drag settings.
        protected override void Start()
        {
            base.Start();
            rb.linearDamping = drag;
        }

        // -------------------------
        // Speed Calculation
        // -------------------------
        // Calculates desired movement speed based on walk/crouch/sprint states.
        float GetTargetSpeed()
        {
            // Jog = baseSpeed
            float speed = baseSpeed;

            // Walk is a fixed social speed
            if (isWalking)
            {
                speed = walkSpeed;
            }
            else
            {
                // Crouch modifies jog speed
                if (isCrouching)
                    speed *= crouchMultiplier;

                // Sprint modifies jog speed
                if (isSprinting)
                    speed *= sprintMultiplier;
            }

            return speed;
        }

        // -------------------------
        // Normalized Speed for Animator (0-1)
        // -------------------------
        // Converts current horizontal speed into a normalized value for animation blend trees.
        public override float GetHorizontalSpeedPercent()
        {
            Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

            // The fastest intended movement speed (jog * sprint multiplier)
            float animationMaxSpeed = baseSpeed * sprintMultiplier;

            // Mathf.Clamp(value, min, max) keeps a number inside a range.
            // Here it guarantees a valid 0-1 animation parameter.
            return Mathf.Clamp(horizontalVelocity.magnitude / animationMaxSpeed, 0f, 1f);
        }

        // -------------------------
        // Jump Input
        // -------------------------
        // Keeps base jump-queue behavior while allowing future subclass customization.
        public override void Jump()
        {
            base.Jump();
        }

        // Fixed-step physics loop order: movement, speed clamp, turn, then jump impulse.
        protected override void FixedUpdate()
        {
            ApplyMovement();
            ClampVelocity();
            ApplyRotation();
            ApplyJump();
        }

        // -------------------------
        // Movement (Physics)
        // -------------------------
        // Applies acceleration force toward desired horizontal velocity from player input.
        void ApplyMovement()
        {
            Vector3 inputDir = new Vector3(moveInput.x, 0f, moveInput.y);

            // sqrMagnitude is the squared vector length. This check avoids a square root
            // and is ideal for "close to zero" comparisons.
            if (inputDir.sqrMagnitude < 0.01f)
                return;

            // normalized keeps direction but scales length to exactly 1.
            inputDir = inputDir.normalized;

            float targetSpeed = GetTargetSpeed();
            Vector3 desiredVelocity = inputDir * targetSpeed;
            Vector3 currentVelocity = rb.linearVelocity;

            Vector3 velocityChange = new Vector3(
                desiredVelocity.x - currentVelocity.x,
                0f,
                desiredVelocity.z - currentVelocity.z
            );

            rb.AddForce(velocityChange * acceleration, ForceMode.Acceleration);
        }

        // -------------------------
        // Jump (Physics)
        // -------------------------
        // Consumes queued jump input and applies upward impulse if jumps remain.
        void ApplyJump()
        {
            if (!jumpQueued)
                return;

            if (CanJump())
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                jumpsRemaining--;
            }

            jumpQueued = false;
        }

        // -------------------------
        // Velocity Clamp
        // -------------------------
        // Caps horizontal velocity so movement speed stays at or below target speed.
        void ClampVelocity()
        {
            Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            float targetSpeed = GetTargetSpeed();
            float targetSpeedSquared = targetSpeed * targetSpeed;

            // Compare sqrMagnitude to avoid computing magnitude's square root each frame.
            if (horizontalVelocity.sqrMagnitude > targetSpeedSquared)
            {
                // normalized gives unit direction; multiplying by targetSpeed sets exact capped speed.
                horizontalVelocity = horizontalVelocity.normalized * targetSpeed;
                rb.linearVelocity = new Vector3(
                    horizontalVelocity.x,
                    rb.linearVelocity.y,
                    horizontalVelocity.z
                );
            }
        }

        // -------------------------
        // Rotation (Input-Based)
        // -------------------------
        // Rotates character toward input direction using smooth spherical interpolation.
        void ApplyRotation()
        {
            Vector3 inputDir = new Vector3(moveInput.x, 0f, moveInput.y);

            if (inputDir.sqrMagnitude > 0.01f)
            {
                // Quaternion.LookRotation creates a target facing rotation from a direction vector.
                Quaternion targetRotation = Quaternion.LookRotation(inputDir.normalized);

                // Breakdown:
                // transform.rotation                   -> current rotation.
                // targetRotation                      -> desired rotation.
                // physicsRotationSpeed * fixedDelta   -> how much interpolation this frame.
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    physicsRotationSpeed * Time.fixedDeltaTime
                );
            }
        }
    }
}
