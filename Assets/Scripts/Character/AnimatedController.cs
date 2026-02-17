using UnityEngine;

namespace Character
{
    /*
     * Converts gameplay state -> animation state
     * Holds references to:
     *   - Animator
     *   - MovementController
     * Updates:
     *   - Speed
     *   - Grounded state
     *   - Vertical velocity
     *   - NearGround
     *   - Crouching
     *   - CanJump
     * Exposes:
     *   - SetTrigger(string)
     *   - TriggerJump()
     *   - TriggerSlide()
     */
    public class AnimatedController : MonoBehaviour
    {
        [SerializeField] float moveSpeed; // for debugging
        MovementController moveController;
        [SerializeField] Animator animator;

        void Start()
        {
            moveController = GetComponent<MovementController>();

            Debug.Assert(animator, "AnimatedController requires an Animator");
            Debug.Assert(moveController, "AnimatedController requires a MovementController");
        }

        // -------------------------
        // Trigger Helpers
        // -------------------------
        public void SetTrigger(string name)
        {
            animator.SetTrigger(name);
        }

        public void TriggerJump()
        {
            animator.SetTrigger("Jump");
        }

        public void TriggerSlide()
        {
            animator.SetTrigger("DoSlide");
        }

        // -------------------------
        // Animation Updates
        // -------------------------
        void Update()
        {
            // Midterm-required speed parameter
            moveSpeed = moveController.GetHorizontalSpeedPercent();
            animator.SetFloat("moveSpeed", moveSpeed);

            // Advanced animation parameters from your previous project
            animator.SetBool("IsGrounded", moveController.IsGroundedState());
            animator.SetFloat("VerticalVelocity", moveController.VerticalVelocity());
            animator.SetBool("NearGround", moveController.NearGround());
            animator.SetBool("IsCrouching", moveController.IsCrouchingState());
            animator.SetBool("CanJump", moveController.CanJump());

        }
    }
}