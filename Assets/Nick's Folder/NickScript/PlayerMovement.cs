using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

namespace TopDown.Movement
{
    [RequireComponent (typeof(PlayerInput))]

    public class PlayerMovement : Mover
    {

        private void OnMove(InputValue value)
        {
            Vector3 playerInput = new Vector3(value.Get<Vector2>().x, value.Get <Vector2>().y, 0);
            currentInput = playerInput;

            Debug.Log($"[OnMove] Input received:");
        }
    }
}