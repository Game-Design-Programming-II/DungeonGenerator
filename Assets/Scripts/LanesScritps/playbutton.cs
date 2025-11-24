using UnityEngine;
using UnityEngine.SceneManagement;

public class playbutton : MonoBehaviour
{
    [SerializeField] private string scenename;

    public void startgame()
    {
        SceneManager.LoadScene(scenename);
    }


    public void endgame()
    {
        Application.Quit();
    }
}
