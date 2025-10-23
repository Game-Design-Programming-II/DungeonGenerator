using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Door : MonoBehaviour
{
    private bool isOpen = false;

    public void OpenDoor()
    {
        if (isOpen) return;
        isOpen = true;

        var col = GetComponent<Collider2D>();
        if (col) col.enabled = false;

        Debug.Log("Door opened permanently!");
    }

    public void CloseDoorTemporary()
    {
        if (isOpen) return; 
        var col = GetComponent<Collider2D>();
        if (col) col.enabled = true;
    }
}
