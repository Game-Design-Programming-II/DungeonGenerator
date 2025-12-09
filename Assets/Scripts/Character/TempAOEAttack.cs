using UnityEngine;
using Character;
using System.Collections;

//THIS A TEMP SCRIPT I KNOW IT SUCKS AHH

public class TempAOEAttack : MonoBehaviour
{
    [Tooltip("Who do you want this to attack")]
    [SerializeField] private string _tag;
    [SerializeField] private float _damage;

    private void OnEnable()
    {
        if(_tag == "Enemy")
        {
            StartCoroutine("Delay");
        }
    }

    private IEnumerator Delay()
    {
        yield return new WaitForSeconds(0.5f);
        this.gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.tag == "Enemy")
        {
            collision.GetComponent<CharacterHealth>().ApplyDamage(_damage, Common.DamageType.None);
        }
        if (collision.tag == "Player")
        {
            collision.GetComponent<PlayerStats>().TakeDamage(_damage);
        }
    }
}
