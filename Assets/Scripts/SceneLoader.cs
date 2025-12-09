using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public string scenename;

    public void loader()
    {
        SceneManager.LoadScene(scenename);
    }

    public void quitter()
    {
        Application.Quit();
    }

}
