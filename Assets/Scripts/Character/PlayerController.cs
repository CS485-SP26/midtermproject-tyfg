using UnityEngine;
using UnityEngine.InputSystem;
using Farming;

/*
* Handles player input and connects it to movement and interactions.
* Exposes input callbacks for:
*   - Movement (OnMove)
*   - Jumping (OnJump)
*   - Interacting (OnInteract)
*   - Sprinting (OnSprint)
*   - Walk toggle (OnWalkToggle)
*   - Crouching (OnCrouch)
*/

namespace Character
{
    [RequireComponent(typeof(PlayerInput))] 
    public class PlayerController : MonoBehaviour
    {
        // Tile selection source used for farming interactions.
        [SerializeField] private TileSelector tileSelector;

        // Cached component dependencies.
        MovementController moveController;
        AnimatedController animatedController;
        Farmer farmer;

        // Resolves required components at startup.
        void Start()
        {
            moveController = GetComponent<MovementController>();
            animatedController = GetComponent<AnimatedController>();
            farmer = GetComponent<Farmer>();
            Debug.Assert(animatedController, "PlayerController requires an AnimatedController");
            Debug.Assert(moveController, "PlayerController requires a MovementController");
            Debug.Assert(tileSelector, "PlayerController requires a TileSelector.");
            Debug.Assert(farmer, "farmer needs farming.");
        }

        // -------------------------
        // Movement
        // -------------------------
        // Forwards movement input to the active movement controller.
        public void OnMove(InputValue inputValue)
        {
            Vector2 inputVector = inputValue.Get<Vector2>();
            moveController.Move(inputVector);
        }

        // Handles jump input with optional farmer stamina gating.
        public void OnJump(InputValue inputValue)
        {
            if (inputValue.isPressed)
            {
                if (moveController == null)
                    return;

                if (farmer == null)
                {
                    moveController.Jump();
                    return;
                }

                if (!moveController.CanJump())
                    return;

                if (farmer.TryConsumeJumpEnergy())
                    moveController.Jump();
            }
        }

        // -------------------------
        // Interaction (Farming)
        // -------------------------
        // Interacts with the currently selected farm tile.
        public void OnInteract(InputValue value)
        {
            FarmTile tile = tileSelector.GetSelectedTile();
            farmer.TryTileInteraction(tile);
            Debug.Log("Interacting with tile: " + tile?.name);
        }

        // -------------------------
        // Extra movement features
        // (from your previous project)
        // -------------------------
        // Enables/disables sprint, delegating to farmer logic when present.
        public void OnSprint(InputValue value)
        {
            if (farmer != null)
            {
                farmer.SetSprintInput(value.isPressed);
                return;
            }

            moveController.SetSprint(value.isPressed);
        }

        // Toggles walk mode on key/button press.
        public void OnWalkToggle(InputValue value)
        {
            if (value.isPressed)
                moveController.ToggleWalk();
        }

        // Toggles crouch state on key/button press.
        public void OnCrouch(InputValue value)
        {
            if (value.isPressed)
                moveController.ToggleCrouch();
        }

        // Optional animation trigger for non-gameplay emote.
        public void OnMacarena(InputValue value)
        {
            if (value.isPressed)
                animatedController.SetTrigger("Macarena");
        }
    }
}
