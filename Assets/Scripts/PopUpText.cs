using System.Collections;
using UnityEngine;
using TMPro;

public class PopUpText : MonoBehaviour
{
    [SerializeField] private GameObject _textPrefab;
    public void PopUp(string text = "Nothing passed in dummy", Color textColor = new Color(), float time = 2f)
    {
        GameObject textInstantance = Instantiate<GameObject>(_textPrefab, gameObject.transform);
        TMP_Text temp = textInstantance.GetComponent<TMP_Text>();
        temp.text = text;
        temp.color = textColor;
        StartCoroutine(Wait(time));
        textInstantance.GetComponent<Animator>().SetBool("Fade", true);
    }

    private IEnumerator Wait(float time)
    {
        yield return new WaitForSeconds(time);
    }

}
