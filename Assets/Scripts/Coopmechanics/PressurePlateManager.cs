using UnityEngine;

public class PressurePlateManager : MonoBehaviour
{
    public PressurePlate[] plates;
    public Door door;

    private bool permanentlyActivated = false;

    void Start()
    {
        foreach (var plate in plates)
        {
            plate.OnPressed.AddListener(CheckPlates);
            plate.OnReleased.AddListener(CheckPlates);
        }
    }

    void CheckPlates()
    {
        if (permanentlyActivated)
            return;

        bool allPressed = true;
        foreach (var plate in plates)
        {
            if (!plate.IsPressed)
            {
                allPressed = false;
                break;
            }
        }

        if (allPressed)
        {
            permanentlyActivated = true;
            door.OpenDoor();
            Debug.Log("Door permanently activated!");
        }
        else
        {
            door.CloseDoorTemporary();
        }
    }
}
