using UnityEngine;

namespace Character
{
    public class PhysicsMovement : MovementController
    {
        [SerializeField] float drag = 0.5f;
        [SerializeField] float physicsRotationSpeed = 10f;

        protected override void Start()
        {
            base.Start();
            rb.linearDamping = drag;
        }

        // -------------------------
        // Speed Calculation
        // -------------------------
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
        // Normalized Speed for Animator (0–1)
        // -------------------------
        public override float GetHorizontalSpeedPercent()
        {
            Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

            // The fastest intended movement speed (jog * sprint multiplier)
            float animationMaxSpeed = baseSpeed * sprintMultiplier;

            return Mathf.Clamp01(horizontalVelocity.magnitude / animationMaxSpeed);
        }

        // -------------------------
        // Jump Input
        // -------------------------
        public override void Jump()
        {
            jumpQueued = true;
        }

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
        void ApplyMovement()
        {
            Vector3 inputDir = new Vector3(moveInput.x, 0f, moveInput.y);

            if (inputDir.sqrMagnitude < 0.01f)
                return;

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
        void ClampVelocity()
        {
            Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            float targetSpeed = GetTargetSpeed();

            if (horizontalVelocity.magnitude > targetSpeed)
            {
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
        void ApplyRotation()
        {
            Vector3 inputDir = new Vector3(moveInput.x, 0f, moveInput.y);

            if (inputDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(inputDir.normalized);

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    physicsRotationSpeed * Time.fixedDeltaTime
                );
            }
        }
    }
}
