using UnityEngine;
using UnityEngine.InputSystem;
using Farming;

namespace Character
{
    [RequireComponent(typeof(PlayerInput))] 
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private TileSelector tileSelector;

        MovementController moveController;
        AnimatedController animatedController;
        Farmer farmer;
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
        public void OnMove(InputValue inputValue)
        {
            Vector2 inputVector = inputValue.Get<Vector2>();
            moveController.Move(inputVector);
        }

        public void OnJump(InputValue inputValue)
        {
            if (inputValue.isPressed)
                moveController.Jump();
        }

        // -------------------------
        // Interaction (Farming)
        // -------------------------
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
        public void OnSprint(InputValue value)
        {
            moveController.SetSprint(value.isPressed);
        }

        public void OnWalkToggle(InputValue value)
        {
            if (value.isPressed)
                moveController.ToggleWalk();
        }

        public void OnCrouch(InputValue value)
        {
            if (value.isPressed)
                moveController.ToggleCrouch();
        }

        // Optional fun animation
        public void OnMacarena(InputValue value)
        {
            if (value.isPressed)
                animatedController.SetTrigger("Macarena");
        }
    }
}