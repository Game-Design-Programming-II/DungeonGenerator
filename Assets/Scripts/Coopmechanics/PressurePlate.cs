using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class PressurePlate : MonoBehaviour
{
    public UnityEvent OnPressed;
    public UnityEvent OnReleased;

    private int playersOnPlate = 0;

    public bool IsPressed => playersOnPlate > 0;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playersOnPlate++;
        if (playersOnPlate == 1)
            OnPressed?.Invoke();

        Debug.Log($"{other.name} stepped on {name}. Players on plate: {playersOnPlate}");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playersOnPlate--;
        if (playersOnPlate <= 0)
        {
            playersOnPlate = 0;
            OnReleased?.Invoke();
        }

        Debug.Log($"{other.name} stepped off {name}. Players on plate: {playersOnPlate}");
    }
}
