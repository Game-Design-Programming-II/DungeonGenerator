using Character;
using UnityEngine;
using System.Collections;

//THIS A TEMP SCRIPT I KNOW IT SUCKS AHH

public class TempAOEHealing : MonoBehaviour
{
    [SerializeField] private float _healing;

    private void OnEnable()
    {
        this.gameObject.GetComponentInParent<PlayerStats>().Heal(_healing);
        StartCoroutine("Delay");
    }

    private IEnumerator Delay()
    {
        yield return new WaitForSeconds(0.5f);
        this.gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        
        if (collision.tag == "Player")
        {
            collision.GetComponent<PlayerStats>().Heal(_healing);
        }
    }
}
