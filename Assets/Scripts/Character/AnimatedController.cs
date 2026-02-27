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
        // Cached speed value sent to animator (also visible in inspector for debugging).
        [SerializeField] float moveSpeed;
        // Movement source used to read runtime locomotion state.
        MovementController moveController;
        // Animator that receives parameter/trigger updates.
        [SerializeField] Animator animator;

        // Caches required components and validates dependencies.
        void Start()
        {
            moveController = GetComponent<MovementController>();

            Debug.Assert(animator, "AnimatedController requires an Animator");
            Debug.Assert(moveController, "AnimatedController requires a MovementController");
        }

        // -------------------------
        // Trigger Helpers
        // -------------------------
        // Generic trigger helper for any animator trigger parameter.
        public void SetTrigger(string name)
        {
            animator.SetTrigger(name);
        }

        // Fires the jump trigger on the animator.
        public void TriggerJump()
        {
            animator.SetTrigger("Jump");
        }

        // Fires the slide trigger on the animator.
        public void TriggerSlide()
        {
            animator.SetTrigger("DoSlide");
        }

        // -------------------------
        // Animation Updates
        // -------------------------
        // Pushes current movement/grounding state to animator parameters each frame.
        void Update()
        {
            // Midterm-required speed parameter.
            moveSpeed = moveController.GetHorizontalSpeedPercent();
            animator.SetFloat("moveSpeed", moveSpeed);

            // Advanced animation parameters from previous project logic.
            animator.SetBool("IsGrounded", moveController.IsGroundedState());
            animator.SetFloat("VerticalVelocity", moveController.VerticalVelocity);
            animator.SetBool("NearGround", moveController.NearGround());
            animator.SetBool("IsCrouching", moveController.IsCrouchingState());
            // TODO: CanJump isn't implemented in unity (i think??)
            animator.SetBool("CanJump", moveController.CanJump());
        }
    }
}
