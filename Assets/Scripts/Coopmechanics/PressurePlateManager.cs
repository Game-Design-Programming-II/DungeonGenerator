using UnityEngine;

public class PressurePlateManager : MonoBehaviour
{
    public PressurePlate[] plates; // Assign all plates here
    public Door door;              // Assign the door here

    private bool permanentlyActivated = false;

    private void Awake()
    {
        // Subscribe to plate events early
        foreach (var plate in plates)
        {
            plate.OnPressed.AddListener(OnPlateStateChanged);
            plate.OnReleased.AddListener(OnPlateStateChanged);
        }
    }

    public void OnPlateStateChanged()
    {
        if (permanentlyActivated) return;

        int pressedCount = 0;
        foreach (var plate in plates)
        {
            if (plate.IsPressed) pressedCount++;
        }

        if (pressedCount == plates.Length)
        {
            permanentlyActivated = true;
            door.OpenDoor();
            Debug.Log("Both players stood on plates — door permanently opened!");
        }
        else
        {
            door.CloseDoorTemporary();
            Debug.Log("Not all plates pressed yet — door closed.");
        }
    }
}
