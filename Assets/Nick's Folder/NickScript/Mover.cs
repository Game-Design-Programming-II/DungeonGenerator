using Unity.VisualScripting;
using UnityEngine;


namespace TopDown.Movement
{
    public class Mover : MonoBehaviour
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        [SerializeField] private float movement_speed;
        private Rigidbody2D body;
        protected Vector3 currentInput;
        // Update is called once per frame
        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
        }

        private void FixedUpdate()
        {
            body.linearVelocity = movement_speed * currentInput * Time.fixedDeltaTime;
        }

    }
}