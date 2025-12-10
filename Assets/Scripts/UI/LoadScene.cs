using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadScene : MonoBehaviour
{
    [SerializeField, TextArea] private string _scene;
    //[SerializeField] private Scene _scene;
    public void Load()
    {
        SceneManager.LoadScene(_scene);
    }
}
