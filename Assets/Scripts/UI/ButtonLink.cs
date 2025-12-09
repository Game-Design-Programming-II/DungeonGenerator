using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonLink : MonoBehaviour
{
    [SerializeField, TextArea] private string _link;
    public void Link()
    {
        Application.OpenURL(_link);
    }
}
